using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Tracking.Contracts;
using TripTracker.Trips.Contracts;

namespace TripTracker.Api.Hooks;

/// <summary>
/// Deletes a trip's route when the trip is deleted (FR-07). It lives in the API project, which already knows both
/// modules, so Tracking does not have to depend on Trips.
/// </summary>
public sealed class TrackingTripDeletionHook : ITripDeletionHook
{
    private readonly ITrackingTripCleanup _cleanup;

    public TrackingTripDeletionHook(ITrackingTripCleanup cleanup)
    {
        _cleanup = cleanup;
    }

    public Task<UnitResult<Error>> OnTripDeletingAsync(Guid tripId, CancellationToken ct) =>
        _cleanup.DeleteAllForTripAsync(tripId, ct);
}
