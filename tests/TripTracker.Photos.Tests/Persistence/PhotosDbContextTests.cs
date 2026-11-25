using Microsoft.EntityFrameworkCore;
using TripTracker.Photos.Domain;
using TripTracker.Photos.Imaging;
using TripTracker.Photos.Persistence;

namespace TripTracker.Photos.Tests.Persistence;

public class PhotosDbContextTests
{
    // Never opened: the model does not connect. Contains no real credentials.
    private const string OfflineConnectionString = "Host=localhost;Database=offline";

    private static Photo NewPhoto(Guid tripId, Guid? markerId, Guid ownerId, long size = 123) =>
        Photo.Create(
            tripId,
            markerId,
            ownerId,
            "a.jpg",
            ImageKind.Jpeg,
            size,
            new ImageFacts(640, 480, 50.1, 30.2, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero)),
            hasThumbnail: true,
            DateTimeOffset.UtcNow);

    [Fact]
    public void Model_UsesThePhotosSchema_AndHasNoForeignKeys()
    {
        using var db = new PhotosDbContext(
            new DbContextOptionsBuilder<PhotosDbContext>().UseNpgsql(OfflineConnectionString).Options);

        var entity = db.Model.FindEntityType(typeof(Photo))!;

        Assert.Equal("photos", entity.GetSchema());
        Assert.Equal("photos", entity.GetTableName());
        Assert.Empty(entity.GetForeignKeys());
    }

    [Fact]
    public async Task Repository_RoundTripsAPhoto_WithAllItsFields()
    {
        var options = new DbContextOptionsBuilder<PhotosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var photo = NewPhoto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await using (var writer = new PhotosDbContext(options))
        {
            await new PhotoRepository(writer).AddAsync(photo, CancellationToken.None);
        }

        await using var reader = new PhotosDbContext(options);
        var loaded = await new PhotoRepository(reader).GetByIdAsync(photo.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(photo.TripId, loaded.TripId);
        Assert.Equal(photo.MarkerId, loaded.MarkerId);
        Assert.Equal(photo.OwnerId, loaded.OwnerId);
        Assert.Equal(photo.OriginalKey, loaded.OriginalKey);
        Assert.Equal(photo.ThumbnailKey, loaded.ThumbnailKey);
        Assert.Equal(640, loaded.Width);
        Assert.Equal(50.1, loaded.Latitude);
        Assert.Equal(photo.TakenAt, loaded.TakenAt);
    }

    [Fact]
    public async Task Repository_ListsCountsAndSums()
    {
        var options = new DbContextOptionsBuilder<PhotosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var trip = Guid.NewGuid();
        var otherTrip = Guid.NewGuid();
        var marker = Guid.NewGuid();
        var owner = Guid.NewGuid();

        await using var db = new PhotosDbContext(options);
        var repository = new PhotoRepository(db);
        await repository.AddAsync(NewPhoto(trip, marker, owner, 100), CancellationToken.None);
        await repository.AddAsync(NewPhoto(trip, marker, owner, 200), CancellationToken.None);
        await repository.AddAsync(NewPhoto(trip, null, owner, 300), CancellationToken.None);
        await repository.AddAsync(NewPhoto(otherTrip, marker, Guid.NewGuid(), 400), CancellationToken.None);

        Assert.Equal(3, (await repository.ListByTripAsync(trip, null, CancellationToken.None)).Count);
        Assert.Equal(2, (await repository.ListByTripAsync(trip, marker, CancellationToken.None)).Count);
        Assert.Equal(2, await repository.CountForMarkerAsync(trip, marker, CancellationToken.None));
        Assert.Equal(600, await repository.SumSizeByOwnerAsync(owner, CancellationToken.None));
        Assert.Equal(0, await repository.SumSizeByOwnerAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Repository_RemovesOnePhotoOrSeveral()
    {
        var options = new DbContextOptionsBuilder<PhotosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var trip = Guid.NewGuid();

        await using var db = new PhotosDbContext(options);
        var repository = new PhotoRepository(db);
        var first = NewPhoto(trip, null, Guid.NewGuid());
        var second = NewPhoto(trip, null, Guid.NewGuid());
        var third = NewPhoto(trip, null, Guid.NewGuid());
        foreach (var photo in new[] { first, second, third })
        {
            await repository.AddAsync(photo, CancellationToken.None);
        }

        await repository.RemoveAsync(first, CancellationToken.None);
        await repository.RemoveRangeAsync([second, third], CancellationToken.None);

        Assert.Empty(await repository.ListByTripAsync(trip, null, CancellationToken.None));
    }
}
