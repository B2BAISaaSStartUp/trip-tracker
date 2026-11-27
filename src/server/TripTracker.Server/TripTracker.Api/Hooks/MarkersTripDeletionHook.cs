using CSharpFunctionalExtensions;
using TripTracker.Markers.Contracts;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Trips.Contracts;

namespace TripTracker.Api.Hooks;

/// <summary>
/// Deletes a trip's markers when the trip is deleted (FR-07). It lives in the API project, which already knows both
/// modules, so Markers does not have to depend on Trips.
/// </summary>
public sealed class MarkersTripDeletionHook : ITripDeletionHook
{
    private readonly IMarkerTripCleanup _cleanup;

    public MarkersTripDeletionHook(IMarkerTripCleanup cleanup)
    {
        _cleanup = cleanup;
    }

    public Task<UnitResult<Error>> OnTripDeletingAsync(Guid tripId, CancellationToken ct) =>
        _cleanup.DeleteAllForTripAsync(tripId, ct);
}
