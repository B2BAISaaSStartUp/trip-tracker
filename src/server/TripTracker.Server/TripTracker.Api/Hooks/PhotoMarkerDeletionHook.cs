using CSharpFunctionalExtensions;
using TripTracker.Markers.Contracts;
using TripTracker.Photos.Contracts;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Api.Hooks;

/// <summary>
/// Detaches a marker's photos before the marker is deleted, so they stay in the trip gallery instead of pointing at
/// a marker that no longer exists. It lives in the API project, so Markers and Photos stay independent.
/// </summary>
public sealed class PhotoMarkerDeletionHook : IMarkerDeletionHook
{
    private readonly IPhotoService _photos;

    public PhotoMarkerDeletionHook(IPhotoService photos)
    {
        _photos = photos;
    }

    public async Task<UnitResult<Error>> OnMarkerDeletingAsync(Guid tripId, Guid markerId, CancellationToken ct)
    {
        var photos = await _photos.ListAsync(tripId, markerId, ct);
        if (photos.IsFailure)
        {
            return photos.Error;
        }

        foreach (var photo in photos.Value)
        {
            var detached = await _photos.AssignMarkerAsync(tripId, photo.Id, null, ct);
            if (detached.IsFailure)
            {
                return detached.Error;
            }
        }

        return UnitResult.Success<Error>();
    }
}
