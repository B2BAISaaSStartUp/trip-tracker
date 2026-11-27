using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Contracts;

/// <summary>
/// Removes every marker of a trip. Meant to be called when the trip is deleted (see <c>ITripDeletionHook</c> in the
/// Trips module); it does no access check, so it must not be exposed to clients.
/// </summary>
public interface IMarkerTripCleanup
{
    Task<UnitResult<Error>> DeleteAllForTripAsync(Guid tripId, CancellationToken ct);
}
