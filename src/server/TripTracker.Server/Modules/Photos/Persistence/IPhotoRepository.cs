using TripTracker.Photos.Domain;

namespace TripTracker.Photos.Persistence;

/// <summary>Storage for photo metadata. The bytes are kept by <see cref="Storage.IObjectStore"/>.</summary>
internal interface IPhotoRepository
{
    Task<Photo?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Photos of a trip, oldest upload first; limited to one marker when <paramref name="markerId"/> is given.</summary>
    Task<IReadOnlyList<Photo>> ListByTripAsync(Guid tripId, Guid? markerId, CancellationToken ct);

    Task<int> CountForMarkerAsync(Guid tripId, Guid markerId, CancellationToken ct);

    /// <summary>Total size in bytes of everything the user has uploaded.</summary>
    Task<long> SumSizeByOwnerAsync(Guid ownerId, CancellationToken ct);

    Task AddAsync(Photo photo, CancellationToken ct);

    /// <summary>Saves changes made to a photo returned by this repository.</summary>
    Task SaveAsync(Photo photo, CancellationToken ct);

    Task RemoveAsync(Photo photo, CancellationToken ct);

    Task RemoveRangeAsync(IReadOnlyCollection<Photo> photos, CancellationToken ct);
}
