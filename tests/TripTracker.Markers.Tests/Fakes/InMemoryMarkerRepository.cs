using TripTracker.Markers.Domain;
using TripTracker.Markers.Persistence;

namespace TripTracker.Markers.Tests.Fakes;

internal sealed class InMemoryMarkerRepository : IMarkerRepository
{
    private readonly List<Marker> _markers = [];

    public IReadOnlyList<Marker> All => _markers;

    public Task<Marker?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_markers.FirstOrDefault(m => m.Id == id));

    public Task<IReadOnlyList<Marker>> ListByTripAsync(Guid tripId, CancellationToken ct)
    {
        IReadOnlyList<Marker> markers = _markers
            .Where(m => m.TripId == tripId)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        return Task.FromResult(markers);
    }

    public Task AddAsync(Marker marker, CancellationToken ct)
    {
        _markers.Add(marker);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Marker marker, CancellationToken ct) => Task.CompletedTask;

    public Task RemoveAsync(Marker marker, CancellationToken ct)
    {
        _markers.Remove(marker);
        return Task.CompletedTask;
    }

    public Task RemoveAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        _markers.RemoveAll(m => m.TripId == tripId);
        return Task.CompletedTask;
    }
}
