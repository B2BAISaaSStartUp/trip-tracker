using Microsoft.EntityFrameworkCore;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Persistence;

/// <summary>EF Core implementation of <see cref="ITripRepository"/>.</summary>
internal sealed class TripRepository : ITripRepository
{
    private readonly TripsDbContext _db;

    public TripRepository(TripsDbContext db)
    {
        _db = db;
    }

    public async Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.Trips.FindAsync([id], ct);

    public Task<Trip?> GetByShareTokenAsync(string shareToken, CancellationToken ct) =>
        _db.Trips.FirstOrDefaultAsync(t => t.ShareToken == shareToken, ct);

    public async Task<PagedList<Trip>> ListByOwnerAsync(Guid ownerId, ListTripsQuery query, CancellationToken ct)
    {
        var trips = _db.Trips.AsNoTracking().Where(t => t.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            trips = trips.Where(t => EF.Functions.ILike(EF.Property<string>(t, Trip.TitleFieldName), pattern));
        }

        var ordered = query.NewestFirst
            ? trips.OrderByDescending(t => t.StartDate).ThenByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
            : trips.OrderBy(t => t.StartDate).ThenBy(t => t.CreatedAt).ThenBy(t => t.Id);

        var total = await trips.LongCountAsync(ct);
        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedList<Trip>
        {
            Items = items,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task AddAsync(Trip trip, CancellationToken ct)
    {
        _db.Trips.Add(trip);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(Trip trip, CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task RemoveAsync(Trip trip, CancellationToken ct)
    {
        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync(ct);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
