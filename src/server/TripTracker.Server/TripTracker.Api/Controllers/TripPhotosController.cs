using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TripTracker.Photos.Contracts;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Framework.EndpointResults;
using TripTracker.SharedKernel.TripTracker.Photos;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Trips.Contracts;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace TripTracker.Api.Controllers;

/// <summary>Form fields of a photo upload.</summary>
public sealed class UploadPhotoForm
{
    /// <summary>The image file (JPEG, PNG or HEIC, up to 15 MB).</summary>
    public IFormFile? File { get; init; }

    /// <summary>Marker to attach the photo to; omit it for the trip gallery only.</summary>
    public Guid? MarkerId { get; init; }
}

/// <summary>
/// HTTP endpoints for the photos of a trip. This is the trust boundary: every action first asks
/// <see cref="ITripAccess"/> (Read or Write) and only then calls <see cref="IPhotoService"/>.
/// Guests read through a share link by passing <c>shareToken</c>.
/// </summary>
public sealed class TripPhotosController : ApplicationController
{
    private readonly IPhotoService _photos;
    private readonly ITripAccess _access;
    private readonly ICurrentUser _currentUser;

    public TripPhotosController(IPhotoService photos, ITripAccess access, ICurrentUser currentUser)
    {
        _photos = photos;
        _access = access;
        _currentUser = currentUser;
    }

    /// <summary>Uploads a photo to a trip the signed-in user owns.</summary>
    [HttpPost("~/api/trips/{tripId:guid}/photos")]
    [Authorize]
    [RequestSizeLimit(PhotoLimits.MaxRequestBytes)]
    [Consumes("multipart/form-data")]
    public async Task<EndpointResult<PhotoDto>> Upload(Guid tripId, [FromForm] UploadPhotoForm form, CancellationToken ct)
    {
        if (form.File is null)
        {
            return EndpointResult<PhotoDto>.FromErrors(Errors.Validation.ValueIsRequired("File").ToErrorList());
        }

        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult<PhotoDto>.FromErrors(access.Error.ToErrorList());
        }

        await using var content = form.File.OpenReadStream();
        var result = await _photos.UploadAsync(
            tripId,
            new PhotoUploadRequest(content, form.File.FileName, form.File.Length, form.MarkerId),
            ct);

        return result.IsSuccess
            ? EndpointResult<PhotoDto>.Created($"/api/trips/{tripId}/photos/{result.Value.Id}/file", result.Value)
            : result;
    }

    /// <summary>Lists the photos of a trip (the gallery), or of one marker.</summary>
    [HttpGet("~/api/trips/{tripId:guid}/photos")]
    [AllowAnonymous]
    public async Task<EndpointResult<IReadOnlyList<PhotoDto>>> List(
        Guid tripId,
        [FromQuery] Guid? markerId,
        [FromQuery] string? shareToken,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, shareToken, TripAccessKind.Read, ct);
        if (access.IsFailure)
        {
            return EndpointResult<IReadOnlyList<PhotoDto>>.FromErrors(access.Error.ToErrorList());
        }

        return await _photos.ListAsync(tripId, markerId, ct);
    }

    /// <summary>Downloads a photo. Everyone but the uploader gets it without EXIF metadata.</summary>
    [HttpGet("~/api/trips/{tripId:guid}/photos/{photoId:guid}/file")]
    [AllowAnonymous]
    public Task<IResult> GetFile(Guid tripId, Guid photoId, [FromQuery] string? shareToken, CancellationToken ct) =>
        OpenAsync(tripId, photoId, PhotoVariant.Original, shareToken, ct);

    /// <summary>Downloads the thumbnail of a photo.</summary>
    [HttpGet("~/api/trips/{tripId:guid}/photos/{photoId:guid}/thumbnail")]
    [AllowAnonymous]
    public Task<IResult> GetThumbnail(Guid tripId, Guid photoId, [FromQuery] string? shareToken, CancellationToken ct) =>
        OpenAsync(tripId, photoId, PhotoVariant.Thumbnail, shareToken, ct);

    /// <summary>Attaches a photo to a marker, or detaches it when <c>markerId</c> is null.</summary>
    [HttpPut("~/api/trips/{tripId:guid}/photos/{photoId:guid}/marker")]
    [Authorize]
    public async Task<EndpointResult<PhotoDto>> AssignMarker(
        Guid tripId,
        Guid photoId,
        [FromBody] AssignMarkerRequest request,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult<PhotoDto>.FromErrors(access.Error.ToErrorList());
        }

        return await _photos.AssignMarkerAsync(tripId, photoId, request.MarkerId, ct);
    }

    /// <summary>Deletes a photo.</summary>
    [HttpDelete("~/api/trips/{tripId:guid}/photos/{photoId:guid}")]
    [Authorize]
    public async Task<EndpointResult> Delete(Guid tripId, Guid photoId, CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult.FromErrors(access.Error.ToErrorList());
        }

        return await _photos.DeleteAsync(tripId, photoId, ct);
    }

    private async Task<IResult> OpenAsync(
        Guid tripId,
        Guid photoId,
        PhotoVariant variant,
        string? shareToken,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, shareToken, TripAccessKind.Read, ct);
        if (access.IsFailure)
        {
            return new ErrorsResult(access.Error);
        }

        var content = await _photos.OpenAsync(tripId, photoId, variant, ct);
        if (content.IsFailure)
        {
            return new ErrorsResult(content.Error);
        }

        // Uploaded files are untrusted: never let a browser guess a different type. "private" because the
        // uploader and other viewers receive different bytes for the same URL.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "private, max-age=3600";

        return Results.Stream(content.Value.Content, content.Value.ContentType);
    }

    private Task<UnitResult<Error>> CheckAsync(Guid tripId, string? shareToken, TripAccessKind kind, CancellationToken ct)
    {
        Guid? userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        return _access.CheckAsync(new TripAccessRequest(tripId, userId, shareToken, kind), ct);
    }
}
