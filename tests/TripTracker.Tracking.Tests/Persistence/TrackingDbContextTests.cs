using Microsoft.EntityFrameworkCore;
using TripTracker.Tracking.Domain;
using TripTracker.Tracking.Persistence;

namespace TripTracker.Tracking.Tests.Persistence;

/// <summary>Checks the EF Core mapping without a database: model shape, SQL translation and round trip.</summary>
public class TrackingDbContextTests
{
    // Never opened: ToQueryString and the model do not connect. Contains no real credentials.
    private const string OfflineConnectionString = "Host=localhost;Database=offline";

    private static TrackingDbContext NewNpgsqlContext() =>
        new(new DbContextOptionsBuilder<TrackingDbContext>().UseNpgsql(OfflineConnectionString).Options);

    private static TrackingDbContext NewInMemoryContext(string name) =>
        new(new DbContextOptionsBuilder<TrackingDbContext>().UseInMemoryDatabase(name).Options);

    private static TrackPoint NewPoint(Guid tripId, Guid? clientPointId = null, int minute = 0)
    {
        var now = new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
        return TrackPoint.Create(
            tripId,
            Guid.NewGuid(),
            clientPointId ?? Guid.NewGuid(),
            50.45,
            30.52,
            null,
            null,
            now.AddMinutes(minute - 60),
            now).Value;
    }

    [Fact]
    public void Model_UsesTrackingSchemaAndTable()
    {
        using var db = NewNpgsqlContext();

        var entity = db.Model.FindEntityType(typeof(TrackPoint))!;

        Assert.Equal("tracking", entity.GetSchema());
        Assert.Equal("track_points", entity.GetTableName());
    }

    [Fact]
    public void Model_HasUniqueIndexOnTripAndClientPointId()
    {
        using var db = NewNpgsqlContext();

        var entity = db.Model.FindEntityType(typeof(TrackPoint))!;
        var index = entity.GetIndexes().Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(TrackPoint.TripId), nameof(TrackPoint.ClientPointId) }));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void ExistingPointsQuery_TranslatesToSql()
    {
        using var db = NewNpgsqlContext();
        var tripId = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid() };

        var sql = db.TrackPoints
            .Where(p => p.TripId == tripId && ids.Contains(p.ClientPointId))
            .Select(p => p.ClientPointId)
            .ToQueryString();

        Assert.Contains("FROM tracking.track_points", sql);
    }

    [Fact]
    public async Task AddNew_TwiceWithTheSamePoints_StoresThemOnce()
    {
        var name = Guid.NewGuid().ToString();
        var tripId = Guid.NewGuid();
        var clientPointId = Guid.NewGuid();

        await using (var first = NewInMemoryContext(name))
        {
            var added = await new TrackPointRepository(first).AddNewAsync(tripId, [NewPoint(tripId, clientPointId)], CancellationToken.None);
            Assert.Equal(1, added);
        }

        await using (var second = NewInMemoryContext(name))
        {
            var added = await new TrackPointRepository(second).AddNewAsync(
                tripId,
                [NewPoint(tripId, clientPointId), NewPoint(tripId)],
                CancellationToken.None);
            Assert.Equal(1, added);
        }

        await using var reader = NewInMemoryContext(name);
        var route = await new TrackPointRepository(reader).ListByTripAsync(tripId, CancellationToken.None);
        Assert.Equal(2, route.Count);
    }

    [Fact]
    public async Task ListAndRemove_WorkPerTrip()
    {
        var name = Guid.NewGuid().ToString();
        var trip = Guid.NewGuid();
        var otherTrip = Guid.NewGuid();
        var late = NewPoint(trip, minute: 30);
        var early = NewPoint(trip, minute: 10);

        await using (var writer = NewInMemoryContext(name))
        {
            var repository = new TrackPointRepository(writer);
            await repository.AddNewAsync(trip, [late, early], CancellationToken.None);
            await repository.AddNewAsync(otherTrip, [NewPoint(otherTrip)], CancellationToken.None);
        }

        await using (var reader = NewInMemoryContext(name))
        {
            var route = await new TrackPointRepository(reader).ListByTripAsync(trip, CancellationToken.None);
            Assert.Equal(new[] { early.Id, late.Id }, route.Select(p => p.Id));
        }

        await using (var remover = NewInMemoryContext(name))
        {
            await new TrackPointRepository(remover).RemoveAllForTripAsync(trip, CancellationToken.None);
        }

        await using var check = NewInMemoryContext(name);
        Assert.Empty(await new TrackPointRepository(check).ListByTripAsync(trip, CancellationToken.None));
        Assert.Single(await new TrackPointRepository(check).ListByTripAsync(otherTrip, CancellationToken.None));
    }
}
