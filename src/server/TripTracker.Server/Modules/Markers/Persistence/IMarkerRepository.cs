using TripTracker.Markers.Domain;

namespace TripTracker.Markers.Persistence;

/// <summary>
/// Storage for markers. Everything above the Persistence layer depends only on this interface, so moving
/// to <c>IDataAccess</c> later changes only its implementation.
/// </summary>
internal interface IMarkerRepository
{
    Task<Marker?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Markers of a trip, oldest first.</summary>
    Task<IReadOnlyList<Marker>> ListByTripAsync(Guid tripId, CancellationToken ct);

    Task AddAsync(Marker marker, CancellationToken ct);

    /// <summary>Saves changes made to a marker returned by this repository.</summary>
    Task SaveAsync(Marker marker, CancellationToken ct);

    Task RemoveAsync(Marker marker, CancellationToken ct);

    Task RemoveAllForTripAsync(Guid tripId, CancellationToken ct);
}
