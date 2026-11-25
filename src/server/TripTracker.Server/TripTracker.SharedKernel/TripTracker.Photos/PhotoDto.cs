namespace TripTracker.SharedKernel.TripTracker.Photos;

/// <summary>Photo metadata (FR-16 to FR-18).</summary>
/// <param name="Id">Photo id.</param>
/// <param name="TripId">Trip the photo belongs to.</param>
/// <param name="MarkerId">Marker the photo is attached to, or <c>null</c> when it only belongs to the trip gallery.</param>
/// <param name="FileName">Original file name.</param>
/// <param name="ContentType">image/jpeg, image/png or image/heic.</param>
/// <param name="SizeBytes">Size of the stored original.</param>
/// <param name="Width">Width in pixels after applying orientation; <c>null</c> when the format cannot be decoded (HEIC).</param>
/// <param name="Height">Height in pixels after applying orientation; <c>null</c> when the format cannot be decoded (HEIC).</param>
/// <param name="Latitude">GPS latitude from EXIF (FR-17). Shown to the uploader only (NFR-12).</param>
/// <param name="Longitude">GPS longitude from EXIF (FR-17). Shown to the uploader only (NFR-12).</param>
/// <param name="TakenAt">Capture time from EXIF, wall-clock time without a zone (stored with offset zero). Shown to the uploader only.</param>
/// <param name="HasThumbnail">Whether <c>/thumbnail</c> is available.</param>
/// <param name="UploadedAt">When the photo was uploaded.</param>
public sealed record PhotoDto(
    Guid Id,
    Guid TripId,
    Guid? MarkerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    int? Width,
    int? Height,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? TakenAt,
    bool HasThumbnail,
    DateTimeOffset UploadedAt);
