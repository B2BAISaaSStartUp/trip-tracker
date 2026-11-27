using Microsoft.EntityFrameworkCore;
using Npgsql;
using TripTracker.Tracking.Domain;

namespace TripTracker.Tracking.Persistence;

/// <summary>EF Core implementation of <see cref="ITrackPointRepository"/>.</summary>
internal sealed class TrackPointRepository : ITrackPointRepository
{
    // One retry is enough: after it, every point of the batch is either stored by us or found as stored.
    private const int MaxAttempts = 2;

    private readonly TrackingDbContext _db;

    public TrackPointRepository(TrackingDbContext db)
    {
        _db = db;
    }

    public async Task<int> AddNewAsync(Guid tripId, IReadOnlyList<TrackPoint> points, CancellationToken ct)
    {
        var ids = points.Select(p => p.ClientPointId).ToList();

        for (var attempt = 1; ; attempt++)
        {
            var stored = await _db.TrackPoints
                .AsNoTracking()
                .Where(p => p.TripId == tripId && ids.Contains(p.ClientPointId))
                .Select(p => p.ClientPointId)
                .ToListAsync(ct);
            var storedSet = stored.ToHashSet();

            var fresh = points.Where(p => !storedSet.Contains(p.ClientPointId)).ToList();
            if (fresh.Count == 0)
            {
                return 0;
            }

            _db.TrackPoints.AddRange(fresh);
            try
            {
                await _db.SaveChangesAsync(ct);
                return fresh.Count;
            }
            catch (DbUpdateException e) when (attempt < MaxAttempts && IsUniqueViolation(e))
            {
                // A retry of the same batch stored some of these points between our check and our insert.
                // Forget the failed inserts and check again.
                _db.ChangeTracker.Clear();
            }
        }
    }

    public async Task<IReadOnlyList<TrackPoint>> ListByTripAsync(Guid tripId, CancellationToken ct) =>
        await _db.TrackPoints
            .AsNoTracking()
            .Where(p => p.TripId == tripId)
            .OrderBy(p => p.RecordedAt)
            .ThenBy(p => p.ReceivedAt)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

    public async Task RemoveAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        var points = await _db.TrackPoints.Where(p => p.TripId == tripId).ToListAsync(ct);
        _db.TrackPoints.RemoveRange(points);
        await _db.SaveChangesAsync(ct);
    }

    private static bool IsUniqueViolation(DbUpdateException e) =>
        e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
