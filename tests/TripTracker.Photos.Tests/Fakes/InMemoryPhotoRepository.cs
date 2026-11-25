using TripTracker.Photos.Domain;
using TripTracker.Photos.Persistence;

namespace TripTracker.Photos.Tests.Fakes;

internal sealed class InMemoryPhotoRepository : IPhotoRepository
{
    private readonly List<Photo> _photos = new();

    public bool FailOnAdd { get; set; }

    public IReadOnlyList<Photo> All => _photos;

    public void Seed(Photo photo) => _photos.Add(photo);

    public Task<Photo?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_photos.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Photo>> ListByTripAsync(Guid tripId, Guid? markerId, CancellationToken ct)
    {
        IReadOnlyList<Photo> result = _photos
            .Where(p => p.TripId == tripId && (markerId is null || p.MarkerId == markerId))
            .OrderBy(p => p.UploadedAt)
            .ThenBy(p => p.Id)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<int> CountForMarkerAsync(Guid tripId, Guid markerId, CancellationToken ct) =>
        Task.FromResult(_photos.Count(p => p.TripId == tripId && p.MarkerId == markerId));

    public Task<long> SumSizeByOwnerAsync(Guid ownerId, CancellationToken ct) =>
        Task.FromResult(_photos.Where(p => p.OwnerId == ownerId).Sum(p => p.SizeBytes));

    public Task AddAsync(Photo photo, CancellationToken ct)
    {
        if (FailOnAdd)
        {
            throw new InvalidOperationException("The database is down.");
        }

        _photos.Add(photo);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Photo photo, CancellationToken ct) => Task.CompletedTask;

    public Task RemoveAsync(Photo photo, CancellationToken ct)
    {
        _photos.Remove(photo);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IReadOnlyCollection<Photo> photos, CancellationToken ct)
    {
        foreach (var photo in photos)
        {
            _photos.Remove(photo);
        }

        return Task.CompletedTask;
    }
}
