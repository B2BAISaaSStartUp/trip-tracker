using TripTracker.Tracking.Domain;

namespace TripTracker.Tracking.Persistence;

/// <summary>
/// Storage for route points. Everything above the Persistence layer depends only on this interface, so moving
/// to <c>IDataAccess</c> later changes only its implementation.
/// </summary>
internal interface ITrackPointRepository
{
    /// <summary>
    /// Adds the points of <paramref name="tripId"/> that are not stored yet and returns how many were added.
    /// A point whose <c>ClientPointId</c> is already stored for the trip is skipped, also when another request
    /// stored it a moment ago. <paramref name="points"/> must not repeat a <c>ClientPointId</c>.
    /// </summary>
    Task<int> AddNewAsync(Guid tripId, IReadOnlyList<TrackPoint> points, CancellationToken ct);

    /// <summary>Points of a trip ordered by the time they were recorded.</summary>
    Task<IReadOnlyList<TrackPoint>> ListByTripAsync(Guid tripId, CancellationToken ct);

    Task RemoveAllForTripAsync(Guid tripId, CancellationToken ct);
}
