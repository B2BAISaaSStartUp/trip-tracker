using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Domain;
using TripTracker.Trips.Persistence;

namespace TripTracker.Trips.Tests.Fakes;

internal sealed class InMemoryTripRepository : ITripRepository
{
    private readonly List<Trip> _trips = [];

    public IReadOnlyList<Trip> All => _trips;

    public void Seed(Trip trip) => _trips.Add(trip);

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_trips.FirstOrDefault(t => t.Id == id));

    public Task<Trip?> GetByShareTokenAsync(string shareToken, CancellationToken ct) =>
        Task.FromResult(_trips.FirstOrDefault(t => t.ShareToken == shareToken));

    public Task<PagedList<Trip>> ListByOwnerAsync(Guid ownerId, ListTripsQuery query, CancellationToken ct)
    {
        var trips = _trips.Where(t => t.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            trips = trips.Where(t => t.Title.Value.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var filtered = (query.NewestFirst
            ? trips.OrderByDescending(t => t.StartDate)
            : trips.OrderBy(t => t.StartDate)).ToList();

        return Task.FromResult(new PagedList<Trip>
        {
            Items = filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList(),
            TotalCount = filtered.Count,
            Page = query.Page,
            PageSize = query.PageSize,
        });
    }

    public Task AddAsync(Trip trip, CancellationToken ct)
    {
        _trips.Add(trip);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Trip trip, CancellationToken ct) => Task.CompletedTask;

    public Task RemoveAsync(Trip trip, CancellationToken ct)
    {
        _trips.Remove(trip);
        return Task.CompletedTask;
    }
}
