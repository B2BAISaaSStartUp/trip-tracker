using TripTracker.Photos.Domain;
using TripTracker.SharedKernel.TripTracker.Photos;

namespace TripTracker.Photos.Application;

internal static class PhotoMapping
{
    /// <summary>Maps a photo. GPS position and capture time are included only when <paramref name="includeLocation"/> is set (NFR-12).</summary>
    public static PhotoDto ToDto(this Photo photo, bool includeLocation) =>
        new(
            photo.Id,
            photo.TripId,
            photo.MarkerId,
            photo.FileName,
            photo.ContentType,
            photo.SizeBytes,
            photo.Width,
            photo.Height,
            includeLocation ? photo.Latitude : null,
            includeLocation ? photo.Longitude : null,
            includeLocation ? photo.TakenAt : null,
            photo.ThumbnailKey is not null,
            photo.UploadedAt);
}
