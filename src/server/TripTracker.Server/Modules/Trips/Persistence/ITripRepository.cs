using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Persistence;

/// <summary>
/// Storage for trips. Everything above the Persistence layer depends only on this interface, so moving
/// to <c>IDataAccess</c> later changes only its implementation.
/// </summary>
internal interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct);

    Task<Trip?> GetByShareTokenAsync(string shareToken, CancellationToken ct);

    /// <summary>Returns one page of the owner's trips, filtered and sorted as <paramref name="query"/> says.</summary>
    Task<PagedList<Trip>> ListByOwnerAsync(Guid ownerId, ListTripsQuery query, CancellationToken ct);

    Task AddAsync(Trip trip, CancellationToken ct);

    /// <summary>Saves changes made to a trip returned by this repository.</summary>
    Task SaveAsync(Trip trip, CancellationToken ct);

    Task RemoveAsync(Trip trip, CancellationToken ct);
}
