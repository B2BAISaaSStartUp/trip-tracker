using Microsoft.EntityFrameworkCore;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Domain;
using TripTracker.Trips.Persistence;

namespace TripTracker.Trips.Tests.Persistence;

/// <summary>Checks the EF Core mapping without a database: model shape, SQL translation and round trip.</summary>
public class TripsDbContextTests
{
    // Never opened: ToQueryString and the model do not connect. Contains no real credentials.
    private const string OfflineConnectionString = "Host=localhost;Database=offline";

    private static TripsDbContext NewNpgsqlContext() =>
        new(new DbContextOptionsBuilder<TripsDbContext>().UseNpgsql(OfflineConnectionString).Options);

    [Fact]
    public void Model_UsesTripsSchemaAndTitleColumn()
    {
        using var db = NewNpgsqlContext();

        var entity = db.Model.FindEntityType(typeof(Trip))!;

        Assert.Equal("trips", entity.GetSchema());
        Assert.Equal("trips", entity.GetTableName());
        Assert.Equal("title", entity.FindProperty(Trip.TitleFieldName)!.GetColumnName());
    }

    [Fact]
    public void TitleSearch_TranslatesToCaseInsensitiveSql()
    {
        using var db = NewNpgsqlContext();

        var sql = db.Trips
            .Where(t => EF.Functions.ILike(EF.Property<string>(t, Trip.TitleFieldName), "%paris%"))
            .ToQueryString();

        Assert.Contains("ILIKE", sql);
        Assert.Contains("t.title", sql);
        Assert.Contains("FROM trips.trips", sql);
    }

    [Fact]
    public async Task Trip_RoundTripsThroughTheContext()
    {
        var options = new DbContextOptionsBuilder<TripsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var trip = Trip.Create(Guid.NewGuid(), "Alps", "Hiking", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5), DateTimeOffset.UtcNow).Value;
        trip.ChangeAccessLevel(TripAccessLevel.ByLink, DateTimeOffset.UtcNow);

        await using (var writer = new TripsDbContext(options))
        {
            await new TripRepository(writer).AddAsync(trip, CancellationToken.None);
        }

        await using var reader = new TripsDbContext(options);
        var loaded = await new TripRepository(reader).GetByShareTokenAsync(trip.ShareToken, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(trip.Id, loaded.Id);
        Assert.Equal("Alps", loaded.Title.Value);
        Assert.Equal(TripAccessLevel.ByLink, loaded.AccessLevel);
        Assert.Equal(new DateOnly(2026, 7, 5), loaded.EndDate);
    }
}
