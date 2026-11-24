using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Tests.Fakes;

namespace TripTracker.Trips.Tests.Application;

public class TripServiceTests
{
    private readonly TripsTestBed _bed = new();

    private static CreateTripRequest NewTrip(string title = "Alps") =>
        new(title, "Hiking", TripsTestBed.Start, TripsTestBed.End);

    private static UpdateTripRequest NewUpdate(string title = "Renamed") =>
        new(title, null, TripsTestBed.Start, TripsTestBed.End);

    [Fact]
    public async Task Create_AsSignedInUser_ReturnsPrivateTripOwnedByUser_AndListsItImmediately()
    {
        _bed.User.SignInAs(TripsTestBed.Owner);

        var created = await _bed.Service.CreateAsync(NewTrip(), CancellationToken.None);
        var list = await _bed.Service.ListAsync(new ListTripsQuery(), CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal(TripAccessLevel.Private, created.Value.AccessLevel);
        Assert.Equal(TripsTestBed.Owner, created.Value.OwnerId);
        Assert.False(string.IsNullOrEmpty(created.Value.ShareToken));
        Assert.Contains(list.Value.Items, t => t.Id == created.Value.Id);
    }

    [Fact]
    public async Task Create_WithEmptyTitle_IsRejectedAndNothingIsStored()
    {
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.CreateAsync(NewTrip(" "), CancellationToken.None);

        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Create_WithEndBeforeStart_IsRejected()
    {
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.CreateAsync(
            new CreateTripRequest("Alps", null, TripsTestBed.End, TripsTestBed.Start),
            CancellationToken.None);

        Assert.Equal("trip.dates.invalid", result.Error.Code);
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Create_WhenNotSignedIn_IsForbidden()
    {
        var result = await _bed.Service.CreateAsync(NewTrip(), CancellationToken.None);

        Assert.Equal(ErrorType.FORBIDDEN, result.Error.Type);
    }

    [Fact]
    public async Task Get_OwnerSeesPrivateTripWithShareToken()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.GetAsync(trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(trip.ShareToken, result.Value.ShareToken);
    }

    [Fact]
    public async Task Get_OtherUserDoesNotSeePrivateTrip()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Stranger);

        var result = await _bed.Service.GetAsync(trip.Id, CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task Get_PublicTripByStranger_DoesNotExposeShareToken()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);
        _bed.User.SignInAs(TripsTestBed.Stranger);

        var result = await _bed.Service.GetAsync(trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ShareToken);
    }

    [Fact]
    public async Task Update_ByOwner_ChangesFields()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.UpdateAsync(trip.Id, NewUpdate("Dolomites"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dolomites", result.Value.Title);
        Assert.Equal("Dolomites", trip.Title.Value);
    }

    [Fact]
    public async Task Update_WithInvalidDates_IsRejected()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.UpdateAsync(
            trip.Id,
            new UpdateTripRequest("Alps", null, TripsTestBed.End, TripsTestBed.Start),
            CancellationToken.None);

        Assert.Equal("trip.dates.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Update_ByNonOwnerOfPublicTrip_IsForbiddenAndTripUnchanged()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);
        _bed.User.SignInAs(TripsTestBed.Stranger);

        var result = await _bed.Service.UpdateAsync(trip.Id, NewUpdate(), CancellationToken.None);

        Assert.Equal(ErrorType.FORBIDDEN, result.Error.Type);
        Assert.Equal("Alps", trip.Title.Value);
    }

    [Fact]
    public async Task Update_ByNonOwnerOfPrivateTrip_IsNotFoundSoExistenceIsNotLeaked()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Stranger);

        var result = await _bed.Service.UpdateAsync(trip.Id, NewUpdate(), CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
        Assert.Equal("Alps", trip.Title.Value);
    }

    [Fact]
    public async Task Delete_ByNonOwnerOfPublicTrip_IsForbiddenAndTripRemains()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.Public);
        _bed.User.SignInAs(TripsTestBed.Stranger);

        var result = await _bed.Service.DeleteAsync(trip.Id, CancellationToken.None);

        Assert.Equal(ErrorType.FORBIDDEN, result.Error.Type);
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task Delete_ByNonOwnerOfPrivateTrip_IsNotFoundAndTripRemains()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Stranger);

        var result = await _bed.Service.DeleteAsync(trip.Id, CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task Delete_ByOwner_RemovesTrip_AfterRunningDeletionHooks()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        var hook = new RecordingHook();
        _bed.Hooks.Add(hook);
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.DeleteAsync(trip.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_bed.Repository.All);
        Assert.Equal([trip.Id], hook.Calls);
    }

    [Fact]
    public async Task Delete_WhenAHookFails_KeepsTheTrip()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.Hooks.Add(new RecordingHook(Error.Failure("hook.failed", "boom")));
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.DeleteAsync(trip.Id, CancellationToken.None);

        Assert.Equal("hook.failed", result.Error.Code);
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task ChangeAccessLevel_ByOwner_Works_AndByStrangerIsRefused()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);

        _bed.User.SignInAs(TripsTestBed.Stranger);
        var refused = await _bed.Service.ChangeAccessLevelAsync(trip.Id, TripAccessLevel.Public, CancellationToken.None);

        _bed.User.SignInAs(TripsTestBed.Owner);
        var changed = await _bed.Service.ChangeAccessLevelAsync(trip.Id, TripAccessLevel.ByLink, CancellationToken.None);

        Assert.True(refused.IsFailure);
        Assert.True(changed.IsSuccess);
        Assert.Equal(TripAccessLevel.ByLink, trip.AccessLevel);
    }

    [Fact]
    public async Task ChangeAccessLevel_WithUnknownValue_IsRejected()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.ChangeAccessLevelAsync(trip.Id, (TripAccessLevel)42, CancellationToken.None);

        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
    }

    [Fact]
    public async Task GetByShareToken_ReturnsGuestViewForByLinkAndPublicTrips()
    {
        var byLink = _bed.SeedTrip(TripsTestBed.Owner, "Linked", TripAccessLevel.ByLink);
        var open = _bed.SeedTrip(TripsTestBed.Owner, "Open", TripAccessLevel.Public);

        var linked = await _bed.Service.GetByShareTokenAsync(byLink.ShareToken, CancellationToken.None);
        var published = await _bed.Service.GetByShareTokenAsync(open.ShareToken, CancellationToken.None);

        Assert.Equal("Linked", linked.Value.Title);
        Assert.Equal("Open", published.Value.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-real-token")]
    public async Task GetByShareToken_WithUnknownToken_IsNotFound(string token)
    {
        _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.ByLink);

        var result = await _bed.Service.GetByShareTokenAsync(token, CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task GetByShareToken_ForPrivateTrip_IsRefused()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner);

        var result = await _bed.Service.GetByShareTokenAsync(trip.ShareToken, CancellationToken.None);

        Assert.Equal(ErrorType.NOT_FOUND, result.Error.Type);
    }

    [Fact]
    public async Task GetByShareToken_AfterOwnerMakesTripPrivateAgain_IsRefused()
    {
        var trip = _bed.SeedTrip(TripsTestBed.Owner, level: TripAccessLevel.ByLink);
        _bed.User.SignInAs(TripsTestBed.Owner);
        await _bed.Service.ChangeAccessLevelAsync(trip.Id, TripAccessLevel.Private, CancellationToken.None);
        _bed.User.SignOut();

        var result = await _bed.Service.GetByShareTokenAsync(trip.ShareToken, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnTrips_NewestFirstByDefault()
    {
        var older = _bed.SeedTrip(TripsTestBed.Owner, "Older", start: new DateOnly(2025, 1, 1));
        var newer = _bed.SeedTrip(TripsTestBed.Owner, "Newer", start: new DateOnly(2026, 1, 1));
        _bed.SeedTrip(TripsTestBed.Stranger, "Not mine");
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.ListAsync(new ListTripsQuery(), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], result.Value.Items.Select(t => t.Id));
        Assert.Equal(2, result.Value.TotalCount);
    }

    [Fact]
    public async Task List_CanSortOldestFirst()
    {
        var older = _bed.SeedTrip(TripsTestBed.Owner, "Older", start: new DateOnly(2025, 1, 1));
        var newer = _bed.SeedTrip(TripsTestBed.Owner, "Newer", start: new DateOnly(2026, 1, 1));
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.ListAsync(new ListTripsQuery { NewestFirst = false }, CancellationToken.None);

        Assert.Equal([older.Id, newer.Id], result.Value.Items.Select(t => t.Id));
    }

    [Theory]
    [InlineData("paris")]
    [InlineData("PARIS")]
    [InlineData("  PaRiS  ")]
    public async Task List_SearchIgnoresCase(string search)
    {
        _bed.SeedTrip(TripsTestBed.Owner, "Weekend in Paris");
        _bed.SeedTrip(TripsTestBed.Owner, "Alps");
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.ListAsync(new ListTripsQuery { Search = search }, CancellationToken.None);

        var item = Assert.Single(result.Value.Items);
        Assert.Equal("Weekend in Paris", item.Title);
    }

    [Fact]
    public async Task List_Paging_SplitsResultsAndReportsNextPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            _bed.SeedTrip(TripsTestBed.Owner, $"Trip {i}", start: new DateOnly(2026, i, 1));
        }

        _bed.User.SignInAs(TripsTestBed.Owner);

        var first = await _bed.Service.ListAsync(new ListTripsQuery { Page = 1, PageSize = 2 }, CancellationToken.None);
        var last = await _bed.Service.ListAsync(new ListTripsQuery { Page = 3, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(["Trip 5", "Trip 4"], first.Value.Items.Select(t => t.Title));
        Assert.Equal(5, first.Value.TotalCount);
        Assert.True(first.Value.HasNextPage);
        Assert.False(first.Value.HasPreviousPage);
        Assert.Equal(["Trip 1"], last.Value.Items.Select(t => t.Title));
        Assert.False(last.Value.HasNextPage);
        Assert.True(last.Value.HasPreviousPage);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, ListTripsQuery.MaxPageSize + 1)]
    public async Task List_WithInvalidPaging_IsRejected(int page, int pageSize)
    {
        _bed.User.SignInAs(TripsTestBed.Owner);

        var result = await _bed.Service.ListAsync(new ListTripsQuery { Page = page, PageSize = pageSize }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
    }

    [Fact]
    public async Task List_WhenNotSignedIn_IsForbidden()
    {
        var result = await _bed.Service.ListAsync(new ListTripsQuery(), CancellationToken.None);

        Assert.Equal(ErrorType.FORBIDDEN, result.Error.Type);
    }

    private sealed class RecordingHook : ITripDeletionHook
    {
        private readonly Error? _failure;

        public RecordingHook(Error? failure = null)
        {
            _failure = failure;
        }

        public List<Guid> Calls { get; } = [];

        public Task<UnitResult<Error>> OnTripDeletingAsync(Guid tripId, CancellationToken ct)
        {
            Calls.Add(tripId);
            return Task.FromResult(_failure is null ? UnitResult.Success<Error>() : UnitResult.Failure(_failure));
        }
    }
}
