using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using TripTracker.Photos.Contracts;
using TripTracker.Photos.Domain;
using TripTracker.Photos.Imaging;
using TripTracker.Photos.Persistence;
using TripTracker.Photos.Storage;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Photos;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Application;

internal sealed class PhotoService : IPhotoService, IPhotoTripCleanup
{
    private readonly IPhotoRepository _repository;
    private readonly IObjectStore _store;
    private readonly IImageProcessor _images;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(
        IPhotoRepository repository,
        IObjectStore store,
        IImageProcessor images,
        ICurrentUser currentUser,
        TimeProvider clock,
        ILogger<PhotoService> logger)
    {
        _repository = repository;
        _store = store;
        _images = images;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<PhotoDto, Error>> UploadAsync(Guid tripId, PhotoUploadRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return PhotoErrors.NotAuthenticated();
        }

        var userId = _currentUser.UserId;

        if (request.Length <= 0)
        {
            return PhotoErrors.FileRequired();
        }

        if (request.Length > PhotoLimits.MaxFileSizeBytes)
        {
            return PhotoErrors.TooLarge();
        }

        // The declared length is not trusted: the stream is read with a hard cap.
        var data = await ReadAtMostAsync(request.Content, PhotoLimits.MaxFileSizeBytes, ct);
        if (data is null)
        {
            return PhotoErrors.TooLarge();
        }

        if (data.Length == 0)
        {
            return PhotoErrors.FileRequired();
        }

        var detected = ImageFormatDetector.Detect(data);
        if (detected is null)
        {
            return PhotoErrors.UnsupportedFormat();
        }

        var kind = detected.Value;

        if (request.MarkerId is { } markerId
            && await _repository.CountForMarkerAsync(tripId, markerId, ct) >= PhotoLimits.MaxPhotosPerMarker)
        {
            return PhotoErrors.MarkerLimit();
        }

        var used = await _repository.SumSizeByOwnerAsync(userId, ct);
        if (used + data.Length > PhotoLimits.UserQuotaBytes)
        {
            return PhotoErrors.QuotaExceeded();
        }

        var processed = _images.Process(data, kind);
        if (processed.IsFailure)
        {
            return processed.Error;
        }

        var thumbnail = processed.Value.Thumbnail;
        var photo = Photo.Create(
            tripId,
            request.MarkerId,
            userId,
            request.FileName,
            kind,
            data.Length,
            processed.Value.Facts,
            hasThumbnail: thumbnail is not null,
            _clock.GetUtcNow());

        try
        {
            await _store.PutAsync(photo.OriginalKey, data, photo.ContentType, ct);
            if (thumbnail is not null && photo.ThumbnailKey is not null)
            {
                await _store.PutAsync(photo.ThumbnailKey, thumbnail, photo.ContentType, ct);
            }

            await _repository.AddAsync(photo, ct);
        }
        catch
        {
            // Do not leave files behind that no record points to.
            await TryDeleteObjectsAsync(photo);
            throw;
        }

        return photo.ToDto(includeLocation: true);
    }

    public async Task<Result<IReadOnlyList<PhotoDto>, Error>> ListAsync(Guid tripId, Guid? markerId, CancellationToken ct)
    {
        var photos = await _repository.ListByTripAsync(tripId, markerId, ct);
        var viewer = ViewerId();

        IReadOnlyList<PhotoDto> dtos = photos
            .Select(p => p.ToDto(includeLocation: viewer == p.OwnerId))
            .ToList();

        return Result.Success<IReadOnlyList<PhotoDto>, Error>(dtos);
    }

    public async Task<Result<PhotoContent, Error>> OpenAsync(
        Guid tripId,
        Guid photoId,
        PhotoVariant variant,
        CancellationToken ct)
    {
        var photo = await _repository.GetByIdAsync(photoId, ct);
        if (photo is null || photo.TripId != tripId)
        {
            return PhotoErrors.NotFound();
        }

        if (variant == PhotoVariant.Thumbnail)
        {
            if (photo.ThumbnailKey is null)
            {
                return PhotoErrors.ThumbnailUnavailable();
            }

            var thumbnail = await TryOpenAsync(photo.ThumbnailKey, ct);
            if (thumbnail is null)
            {
                return PhotoErrors.FileMissing();
            }

            return new PhotoContent(thumbnail, photo.ContentType);
        }

        if (ViewerId() == photo.OwnerId)
        {
            var original = await TryOpenAsync(photo.OriginalKey, ct);
            if (original is null)
            {
                return PhotoErrors.FileMissing();
            }

            return new PhotoContent(original, photo.ContentType);
        }

        // Anyone else gets a copy without EXIF, so a shared trip does not reveal where a photo was taken.
        var kind = ImageKinds.FromContentType(photo.ContentType);
        if (!kind.CanDecode())
        {
            return PhotoErrors.OriginalUnavailable();
        }

        var source = await TryOpenAsync(photo.OriginalKey, ct);
        if (source is null)
        {
            return PhotoErrors.FileMissing();
        }

        byte[] bytes;
        await using (source)
        {
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, ct);
            bytes = buffer.ToArray();
        }

        var stripped = _images.StripMetadata(bytes, kind);
        if (stripped.IsFailure)
        {
            return stripped.Error;
        }

        return new PhotoContent(new MemoryStream(stripped.Value, writable: false), photo.ContentType);
    }

    public async Task<Result<PhotoDto, Error>> AssignMarkerAsync(
        Guid tripId,
        Guid photoId,
        Guid? markerId,
        CancellationToken ct)
    {
        var loaded = await LoadOwnPhotoAsync(tripId, photoId, ct);
        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        var photo = loaded.Value;

        if (markerId is { } marker
            && marker != photo.MarkerId
            && await _repository.CountForMarkerAsync(tripId, marker, ct) >= PhotoLimits.MaxPhotosPerMarker)
        {
            return PhotoErrors.MarkerLimit();
        }

        photo.AssignMarker(markerId);
        await _repository.SaveAsync(photo, ct);

        return photo.ToDto(includeLocation: true);
    }

    public async Task<UnitResult<Error>> DeleteAsync(Guid tripId, Guid photoId, CancellationToken ct)
    {
        var loaded = await LoadOwnPhotoAsync(tripId, photoId, ct);
        if (loaded.IsFailure)
        {
            return loaded.Error;
        }

        // The record goes first: a file left behind is only wasted space, a record without a file is a broken photo.
        await _repository.RemoveAsync(loaded.Value, ct);
        await TryDeleteObjectsAsync(loaded.Value);

        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> DeleteAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        var photos = await _repository.ListByTripAsync(tripId, null, ct);
        if (photos.Count == 0)
        {
            return UnitResult.Success<Error>();
        }

        try
        {
            foreach (var photo in photos)
            {
                await DeleteObjectsAsync(photo, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not delete the photo files of trip {TripId}.", tripId);
            return PhotoErrors.StorageUnavailable();
        }

        await _repository.RemoveRangeAsync(photos.ToList(), ct);
        return UnitResult.Success<Error>();
    }

    private async Task<Result<Photo, Error>> LoadOwnPhotoAsync(Guid tripId, Guid photoId, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return PhotoErrors.NotAuthenticated();
        }

        var photo = await _repository.GetByIdAsync(photoId, ct);
        if (photo is null || photo.TripId != tripId)
        {
            return PhotoErrors.NotFound();
        }

        if (photo.OwnerId != _currentUser.UserId)
        {
            return PhotoErrors.NotOwner();
        }

        return photo;
    }

    private Guid? ViewerId() => _currentUser.IsAuthenticated ? _currentUser.UserId : null;

    private async Task<Stream?> TryOpenAsync(string key, CancellationToken ct)
    {
        try
        {
            return await _store.GetAsync(key, ct);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
    }

    private async Task DeleteObjectsAsync(Photo photo, CancellationToken ct)
    {
        await _store.DeleteAsync(photo.OriginalKey, ct);
        if (photo.ThumbnailKey is not null)
        {
            await _store.DeleteAsync(photo.ThumbnailKey, ct);
        }
    }

    private async Task TryDeleteObjectsAsync(Photo photo)
    {
        try
        {
            await DeleteObjectsAsync(photo, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete the files of photo {PhotoId}; they stay in storage unused.", photo.Id);
        }
    }

    private static async Task<byte[]?> ReadAtMostAsync(Stream stream, long maxBytes, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
