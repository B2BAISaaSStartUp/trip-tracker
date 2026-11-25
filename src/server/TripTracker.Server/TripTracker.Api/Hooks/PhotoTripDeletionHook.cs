using CSharpFunctionalExtensions;
using TripTracker.Photos.Contracts;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Trips.Contracts;

namespace TripTracker.Api.Hooks;

/// <summary>
/// Deletes a trip's photos when the trip is deleted (FR-07). It lives in the API project, which already knows both
/// modules, so Photos does not have to depend on Trips.
/// </summary>
public sealed class PhotoTripDeletionHook : ITripDeletionHook
{
    private readonly IPhotoTripCleanup _cleanup;

    public PhotoTripDeletionHook(IPhotoTripCleanup cleanup)
    {
        _cleanup = cleanup;
    }

    public Task<UnitResult<Error>> OnTripDeletingAsync(Guid tripId, CancellationToken ct) =>
        _cleanup.DeleteAllForTripAsync(tripId, ct);
}
