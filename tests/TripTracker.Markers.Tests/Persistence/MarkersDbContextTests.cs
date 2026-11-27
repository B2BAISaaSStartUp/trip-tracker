using Microsoft.EntityFrameworkCore;
using TripTracker.Markers.Domain;
using TripTracker.Markers.Persistence;
using TripTracker.SharedKernel.TripTracker.Markers;

namespace TripTracker.Markers.Tests.Persistence;

/// <summary>Checks the EF Core mapping without a database: model shape, SQL translation and round trip.</summary>
public class MarkersDbContextTests
{
    // Never opened: ToQueryString and the model do not connect. Contains no real credentials.
    private const string OfflineConnectionString = "Host=localhost;Database=offline";

    private static MarkersDbContext NewNpgsqlContext() =>
        new(new DbContextOptionsBuilder<MarkersDbContext>().UseNpgsql(OfflineConnectionString).Options);

    private static MarkersDbContext NewInMemoryContext(string name) =>
        new(new DbContextOptionsBuilder<MarkersDbContext>().UseInMemoryDatabase(name).Options);

    private static Marker NewMarker(Guid tripId, string title = "Old Town") =>
        Marker.Create(tripId, Guid.NewGuid(), title, "Meet here", MarkerCategory.Food, 50.45, 30.52, DateTimeOffset.UtcNow).Value;

    [Fact]
    public void Model_UsesMarkersSchemaTitleColumnAndTextCategory()
    {
        using var db = NewNpgsqlContext();

        var entity = db.Model.FindEntityType(typeof(Marker))!;

        Assert.Equal("markers", entity.GetSchema());
        Assert.Equal("markers", entity.GetTableName());
        Assert.Equal("title", entity.FindProperty(Marker.TitleFieldName)!.GetColumnName());
        Assert.Equal(typeof(string), entity.FindProperty(nameof(Marker.Category))!.GetProviderClrType());
    }

    [Fact]
    public void ListByTrip_TranslatesToSql()
    {
        using var db = NewNpgsqlContext();
        var tripId = Guid.NewGuid();

        var sql = db.Markers.Where(m => m.TripId == tripId).OrderBy(m => m.CreatedAt).ToQueryString();

        Assert.Contains("FROM markers.markers", sql);
    }

    [Fact]
    public async Task Marker_RoundTripsThroughTheContext()
    {
        var name = Guid.NewGuid().ToString();
        var tripId = Guid.NewGuid();
        var marker = NewMarker(tripId);

        await using (var writer = NewInMemoryContext(name))
        {
            await new MarkerRepository(writer).AddAsync(marker, CancellationToken.None);
        }

        await using var reader = NewInMemoryContext(name);
        var loaded = await new MarkerRepository(reader).GetByIdAsync(marker.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Old Town", loaded.Title.Value);
        Assert.Equal("Meet here", loaded.Description);
        Assert.Equal(MarkerCategory.Food, loaded.Category);
        Assert.Equal(tripId, loaded.TripId);
    }

    [Fact]
    public async Task RemoveAllForTrip_KeepsOtherTrips()
    {
        var name = Guid.NewGuid().ToString();
        var trip = Guid.NewGuid();
        var otherTrip = Guid.NewGuid();

        await using (var writer = NewInMemoryContext(name))
        {
            var repository = new MarkerRepository(writer);
            await repository.AddAsync(NewMarker(trip), CancellationToken.None);
            await repository.AddAsync(NewMarker(otherTrip), CancellationToken.None);
        }

        await using (var remover = NewInMemoryContext(name))
        {
            await new MarkerRepository(remover).RemoveAllForTripAsync(trip, CancellationToken.None);
        }

        await using var check = NewInMemoryContext(name);
        Assert.Empty(await new MarkerRepository(check).ListByTripAsync(trip, CancellationToken.None));
        Assert.Single(await new MarkerRepository(check).ListByTripAsync(otherTrip, CancellationToken.None));
    }
}
