using TripTracker.Photos.Contracts;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Domain;

/// <summary>Errors produced by the Photos module.</summary>
internal static class PhotoErrors
{
    public static Error NotFound() =>
        Error.NotFound("photo.not.found", "Photo was not found");

    public static Error NotAuthenticated() =>
        Error.Unauthorized("photo.user.not.authenticated", "You must be signed in to do this");

    public static Error NotOwner() =>
        Error.Forbidden("photo.access.forbidden", "You do not have permission to change this photo");

    public static Error FileRequired() =>
        Error.Validation("photo.file.required", "A file is required", "File");

    public static Error TooLarge() =>
        Error.Validation(
            "photo.file.too.large",
            $"The file must not exceed {PhotoLimits.MaxFileSizeBytes / (1024 * 1024)} MB",
            "File");

    public static Error TooManyPixels(long maxPixels) =>
        Error.Validation("photo.image.too.large", $"The image must not exceed {maxPixels / 1_000_000} megapixels", "File");

    public static Error UnsupportedFormat() =>
        Error.Validation("photo.format.unsupported", "Only JPEG, PNG and HEIC images are accepted", "File");

    public static Error Corrupt() =>
        Error.Validation("photo.file.corrupt", "The file is not a valid image", "File");

    public static Error MarkerLimit() =>
        Error.Conflict(
            "photo.marker.limit",
            $"A marker can have at most {PhotoLimits.MaxPhotosPerMarker} photos");

    public static Error QuotaExceeded() =>
        Error.Conflict(
            "photo.quota.exceeded",
            $"Your photo storage is limited to {PhotoLimits.UserQuotaBytes / (1024L * 1024 * 1024)} GB");

    public static Error OriginalUnavailable() =>
        Error.Forbidden(
            "photo.original.unavailable",
            "The original of this photo is available only to the person who uploaded it");

    public static Error ThumbnailUnavailable() =>
        Error.NotFound("photo.thumbnail.unavailable", "This photo has no thumbnail");

    public static Error FileMissing() =>
        Error.NotFound("photo.file.missing", "The photo file is missing from storage");

    public static Error StorageUnavailable() =>
        Error.Failure("photo.storage.unavailable", "The photo storage is not available; try again later");
}
