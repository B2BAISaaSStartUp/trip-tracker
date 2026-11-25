using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Photos;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Contracts;

/// <summary>
/// Photos of a trip (FR-16 to FR-18). These methods do not check access to the trip: the caller (the REST layer)
/// must first ask <c>ITripAccess</c> - Read for reading, Write for everything else. The service itself still
/// requires a signed-in user for changes and only lets the uploader change or delete a photo.
/// </summary>
public interface IPhotoService
{
    /// <summary>Stores a JPEG, PNG or HEIC photo of up to 15 MB for the current user.</summary>
    Task<Result<PhotoDto, Error>> UploadAsync(Guid tripId, PhotoUploadRequest request, CancellationToken ct);

    /// <summary>Lists the photos of a trip, or of one marker when <paramref name="markerId"/> is given. GPS data is shown only to the uploader.</summary>
    Task<Result<IReadOnlyList<PhotoDto>, Error>> ListAsync(Guid tripId, Guid? markerId, CancellationToken ct);

    /// <summary>
    /// Opens a photo file. The uploader gets the original untouched; everyone else gets it without EXIF metadata,
    /// and HEIC originals are not served to them because they cannot be cleaned.
    /// </summary>
    Task<Result<PhotoContent, Error>> OpenAsync(Guid tripId, Guid photoId, PhotoVariant variant, CancellationToken ct);

    /// <summary>Attaches a photo to a marker, or detaches it when <paramref name="markerId"/> is <c>null</c> (FR-17).</summary>
    Task<Result<PhotoDto, Error>> AssignMarkerAsync(Guid tripId, Guid photoId, Guid? markerId, CancellationToken ct);

    /// <summary>Deletes a photo and its files.</summary>
    Task<UnitResult<Error>> DeleteAsync(Guid tripId, Guid photoId, CancellationToken ct);
}
