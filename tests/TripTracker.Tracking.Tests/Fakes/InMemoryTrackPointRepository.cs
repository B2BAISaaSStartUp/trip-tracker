using TripTracker.Tracking.Domain;
using TripTracker.Tracking.Persistence;

namespace TripTracker.Tracking.Tests.Fakes;

internal sealed class InMemoryTrackPointRepository : ITrackPointRepository
{
    private readonly List<TrackPoint> _points = [];

    public IReadOnlyList<TrackPoint> All => _points;

    public Task<int> AddNewAsync(Guid tripId, IReadOnlyList<TrackPoint> points, CancellationToken ct)
    {
        var fresh = points
            .Where(p => !_points.Any(s => s.TripId == tripId && s.ClientPointId == p.ClientPointId))
            .ToList();

        _points.AddRange(fresh);
        return Task.FromResult(fresh.Count);
    }

    public Task<IReadOnlyList<TrackPoint>> ListByTripAsync(Guid tripId, CancellationToken ct)
    {
        IReadOnlyList<TrackPoint> points = _points
            .Where(p => p.TripId == tripId)
            .OrderBy(p => p.RecordedAt)
            .ToList();

        return Task.FromResult(points);
    }

    public Task RemoveAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        _points.RemoveAll(p => p.TripId == tripId);
        return Task.CompletedTask;
    }
}
