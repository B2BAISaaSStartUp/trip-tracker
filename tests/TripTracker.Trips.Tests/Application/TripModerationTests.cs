using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Tests.Fakes;

namespace TripTracker.Trips.Tests.Application;

public class TripModerationTests
{
    private readonly TripsTestBed _bed = new();

    [Theory]
    [InlineData(TripAccessLevel.Private)]
    [InlineData(TripAccessLevel.ByLink)]
    public async Task Hide_NonPublicTrip_IsConflict(TripAccessLevel level)
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: level);

        var result = await _bed.Moderation.HideAsync(trip.Id, CancellationToken.None);

        Assert.Equal(ErrorType.CONFLICT, result.Error.Type);
        Assert.False(trip.IsHidden);
    }

    [Fact]
    public async Task Hide_PublicTrip_HidesIt()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);

        var result = await _bed.Moderation.HideAsync(trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(trip.IsHidden);
    }

    [Fact]
    public async Task Hide_MissingTrip_IsNotFound()
    {
        var result = await _bed.Moderation.HideAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task HiddenPublicTrip_IsNotReadableByGuestByToken()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);
        await _bed.Moderation.HideAsync(trip.Id, CancellationToken.None);

        var result = await _bed.Service.GetByShareTokenAsync(trip.ShareToken, CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task Restore_MakesTripReadableAgain()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);
        await _bed.Moderation.HideAsync(trip.Id, CancellationToken.None);

        var restored = await _bed.Moderation.RestoreAsync(trip.Id, CancellationToken.None);
        var read = await _bed.Service.GetByShareTokenAsync(trip.ShareToken, CancellationToken.None);

        Assert.True(restored.IsSuccess);
        Assert.True(read.IsSuccess);
    }

    [Fact]
    public async Task Restore_OnVisibleTrip_Succeeds()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);

        var result = await _bed.Moderation.RestoreAsync(trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Restore_MissingTrip_IsNotFound()
    {
        var result = await _bed.Moderation.RestoreAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }
}
