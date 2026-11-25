using Microsoft.EntityFrameworkCore;
using TripTracker.Photos.Domain;

namespace TripTracker.Photos.Persistence;

/// <summary>EF Core implementation of <see cref="IPhotoRepository"/>.</summary>
internal sealed class PhotoRepository : IPhotoRepository
{
    private readonly PhotosDbContext _db;

    public PhotoRepository(PhotosDbContext db)
    {
        _db = db;
    }

    public async Task<Photo?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _db.Photos.FindAsync([id], ct);

    public async Task<IReadOnlyList<Photo>> ListByTripAsync(Guid tripId, Guid? markerId, CancellationToken ct)
    {
        var photos = _db.Photos.Where(p => p.TripId == tripId);
        if (markerId is { } marker)
        {
            photos = photos.Where(p => p.MarkerId == marker);
        }

        return await photos.OrderBy(p => p.UploadedAt).ThenBy(p => p.Id).ToListAsync(ct);
    }

    public Task<int> CountForMarkerAsync(Guid tripId, Guid markerId, CancellationToken ct) =>
        _db.Photos.CountAsync(p => p.TripId == tripId && p.MarkerId == markerId, ct);

    public Task<long> SumSizeByOwnerAsync(Guid ownerId, CancellationToken ct) =>
        _db.Photos.Where(p => p.OwnerId == ownerId).SumAsync(p => p.SizeBytes, ct);

    public async Task AddAsync(Photo photo, CancellationToken ct)
    {
        _db.Photos.Add(photo);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(Photo photo, CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task RemoveAsync(Photo photo, CancellationToken ct)
    {
        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveRangeAsync(IReadOnlyCollection<Photo> photos, CancellationToken ct)
    {
        _db.Photos.RemoveRange(photos);
        await _db.SaveChangesAsync(ct);
    }
}
