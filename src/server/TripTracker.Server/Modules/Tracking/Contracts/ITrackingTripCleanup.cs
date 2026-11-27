using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Tracking.Contracts;

/// <summary>
/// Removes the whole route of a trip. Meant to be called when the trip is deleted (see <c>ITripDeletionHook</c> in
/// the Trips module); it does no access check, so it must not be exposed to clients.
/// </summary>
public interface ITrackingTripCleanup
{
    Task<UnitResult<Error>> DeleteAllForTripAsync(Guid tripId, CancellationToken ct);
}
