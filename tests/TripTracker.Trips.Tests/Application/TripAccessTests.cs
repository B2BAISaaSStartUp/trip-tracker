using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Tests.Fakes;

namespace TripTracker.Trips.Tests.Application;

public class TripAccessTests
{
    private readonly TripsTestBed _bed = new();

    private Task<UnitResult<Error>> Check(
        Guid tripId,
        Guid? userId,
        string? token,
        TripAccessKind kind) =>
        _bed.Access.CheckAsync(new TripAccessRequest(tripId, userId, token, kind), CancellationToken.None);

    [Theory]
    [InlineData(TripAccessKind.Read)]
    [InlineData(TripAccessKind.Write)]
    public async Task Owner_CanAccessPrivateTrip(TripAccessKind kind)
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);

        var result = await Check(trip.Id, TripsTestBed.Owner, null, kind);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(TripAccessKind.Read)]
    [InlineData(TripAccessKind.Write)]
    public async Task OtherUser_CannotSeePrivateTrip_AndGetsNotFound(TripAccessKind kind)
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);

        var result = await Check(trip.Id, TripsTestBed.Stranger, null, kind);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task PrivateTrip_IsRefusedToGuestEvenWithItsToken()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);

        var result = await Check(trip.Id, null, trip.ShareToken, TripAccessKind.Read);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task Guest_WithCorrectToken_CanReadByLinkTrip()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.ByLink);

        var result = await Check(trip.Id, null, trip.ShareToken, TripAccessKind.Read);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Guest_WithCorrectToken_CannotWriteByLinkTrip()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.ByLink);

        var result = await Check(trip.Id, null, trip.ShareToken, TripAccessKind.Write);

        Assert.Equal(ErrorType.FORBIDDEN, result.Error.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-token")]
    public async Task Guest_WithMissingOrWrongToken_CannotReadByLinkTrip(string? token)
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.ByLink);

        var result = await Check(trip.Id, null, token, TripAccessKind.Read);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task SignedInStranger_WithoutToken_CannotReadByLinkTrip()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.ByLink);

        var result = await Check(trip.Id, TripsTestBed.Stranger, null, TripAccessKind.Read);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task Anyone_CanReadPublicTrip_ButNotWriteIt()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);

        var guestRead = await Check(trip.Id, null, null, TripAccessKind.Read);
        var strangerRead = await Check(trip.Id, TripsTestBed.Stranger, null, TripAccessKind.Read);
        var strangerWrite = await Check(trip.Id, TripsTestBed.Stranger, null, TripAccessKind.Write);

        Assert.True(guestRead.IsSuccess);
        Assert.True(strangerRead.IsSuccess);
        Assert.Equal(ErrorType.FORBIDDEN, strangerWrite.Error.Type);
    }

    [Fact]
    public async Task MissingTrip_IsNotFound()
    {
        var result = await Check(Guid.NewGuid(), TripsTestBed.Owner, null, TripAccessKind.Read);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task HiddenTrip_IsReadableOnlyByOwner()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);
        Assert.True((await _bed.Moderation.HideAsync(trip.Id, CancellationToken.None)).IsSuccess);

        var owner = await Check(trip.Id, TripsTestBed.Owner, null, TripAccessKind.Read);
        var guest = await Check(trip.Id, null, trip.ShareToken, TripAccessKind.Read);
        var stranger = await Check(trip.Id, TripsTestBed.Stranger, null, TripAccessKind.Read);

        Assert.True(owner.IsSuccess);
        Assert.Equal(ErrorType.NOT_FOUND, guest.Error.Type);
        Assert.Equal(ErrorType.NOT_FOUND, stranger.Error.Type);
    }
}
