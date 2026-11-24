using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Trips.Contracts;

/// <summary>
/// EXTENSION POINT (FR-07): deleting a trip must also delete its route, markers and photos, which belong to
/// other modules. Those modules will register an implementation to remove their data. No implementation exists
/// yet, so today deleting a trip removes only the trip itself.
/// </summary>
public interface ITripDeletionHook
{
    /// <summary>Called before the trip is removed. A failure cancels the deletion.</summary>
    Task<UnitResult<Error>> OnTripDeletingAsync(Guid tripId, CancellationToken ct);
}
