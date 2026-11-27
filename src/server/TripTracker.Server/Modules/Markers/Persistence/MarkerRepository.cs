using Microsoft.EntityFrameworkCore;
using TripTracker.Markers.Domain;

namespace TripTracker.Markers.Persistence;

/// <summary>EF Core implementation of <see cref="IMarkerRepository"/>.</summary>
internal sealed class MarkerRepository : IMarkerRepository
{
    private readonly MarkersDbContext _db;

    public MarkerRepository(MarkersDbContext db)
    {
        _db = db;
    }

    public async Task<Marker?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.Markers.FindAsync([id], ct);

    public async Task<IReadOnlyList<Marker>> ListByTripAsync(Guid tripId, CancellationToken ct) =>
        await _db.Markers
            .AsNoTracking()
            .Where(m => m.TripId == tripId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync(ct);

    public async Task AddAsync(Marker marker, CancellationToken ct)
    {
        _db.Markers.Add(marker);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(Marker marker, CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task RemoveAsync(Marker marker, CancellationToken ct)
    {
        _db.Markers.Remove(marker);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        var markers = await _db.Markers.Where(m => m.TripId == tripId).ToListAsync(ct);
        _db.Markers.RemoveRange(markers);
        await _db.SaveChangesAsync(ct);
    }
}
