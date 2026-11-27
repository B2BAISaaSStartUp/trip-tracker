using TripTracker.Markers.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Tests.Application;

public class MarkerServiceTests
{
    private readonly MarkersTestBed _bed = new();

    [Fact]
    public async Task Create_StoresMarkerAndListsIt()
    {
        var created = await _bed.CreateAsync();
        var list = await _bed.Service.ListAsync(MarkersTestBed.Trip, CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.Equal(MarkersTestBed.Trip, created.Value.TripId);
        Assert.Equal("Old Town", created.Value.Title);
        Assert.Equal(MarkerCategory.Sight, created.Value.Category);
        Assert.Equal(MarkersTestBed.Owner, _bed.Repository.All.Single().CreatedBy);
        Assert.Contains(list.Value, m => m.Id == created.Value.Id);
    }

    // Required by the lab instructions: a marker without a title is not created.
    [Fact]
    public async Task Create_WithoutTitle_IsRejectedAndNothingIsStored()
    {
        var result = await _bed.CreateAsync(title: " ");

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "value.is.required");
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Create_WhenNotSignedIn_IsForbidden()
    {
        _bed.User.SignOut();

        var result = await _bed.CreateAsync();

        ErrorAssert.Fails(result, ErrorType.FORBIDDEN, "marker.user.not.authenticated");
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Update_ChangesTheMarker()
    {
        var created = await _bed.CreateAsync();
        _bed.Clock.Advance(TimeSpan.FromHours(1));

        var result = await _bed.Service.UpdateAsync(
            MarkersTestBed.Trip,
            created.Value.Id,
            MarkersTestBed.NewUpdate(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cafe", result.Value.Title);
        Assert.Equal(MarkerCategory.Food, result.Value.Category);
        Assert.Equal(created.Value.CreatedAt.AddHours(1), result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithoutTitle_IsRejected()
    {
        var created = await _bed.CreateAsync();

        var result = await _bed.Service.UpdateAsync(
            MarkersTestBed.Trip,
            created.Value.Id,
            MarkersTestBed.NewUpdate(title: ""),
            CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "value.is.required");
        Assert.Equal("Old Town", _bed.Repository.All.Single().Title.Value);
    }

    [Fact]
    public async Task Update_MarkerOfAnotherTrip_IsNotFound()
    {
        var created = await _bed.CreateAsync(MarkersTestBed.OtherTrip);

        var result = await _bed.Service.UpdateAsync(
            MarkersTestBed.Trip,
            created.Value.Id,
            MarkersTestBed.NewUpdate(),
            CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "marker.not.found");
    }

    [Fact]
    public async Task Update_MissingMarker_IsNotFound()
    {
        var result = await _bed.Service.UpdateAsync(
            MarkersTestBed.Trip,
            Guid.NewGuid(),
            MarkersTestBed.NewUpdate(),
            CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "marker.not.found");
    }

    [Fact]
    public async Task Delete_RunsTheHooksAndRemovesTheMarker()
    {
        var created = await _bed.CreateAsync();

        var result = await _bed.Service.DeleteAsync(MarkersTestBed.Trip, created.Value.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { (MarkersTestBed.Trip, created.Value.Id) }, _bed.Hook.Calls);
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Delete_WhenAHookFails_KeepsTheMarker()
    {
        var created = await _bed.CreateAsync();
        _bed.Hook.FailWith = Error.Failure("photos.unavailable", "Photos could not be detached");

        var result = await _bed.Service.DeleteAsync(MarkersTestBed.Trip, created.Value.Id, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.FAILURE, "photos.unavailable");
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task Delete_WhenNotSignedIn_IsForbidden()
    {
        var created = await _bed.CreateAsync();
        _bed.User.SignOut();

        var result = await _bed.Service.DeleteAsync(MarkersTestBed.Trip, created.Value.Id, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.FORBIDDEN, "marker.user.not.authenticated");
        Assert.Empty(_bed.Hook.Calls);
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task List_ReturnsOnlyThisTripOldestFirst_AlsoForGuests()
    {
        var first = await _bed.CreateAsync(title: "First");
        _bed.Clock.Advance(TimeSpan.FromMinutes(1));
        var second = await _bed.CreateAsync(title: "Second");
        await _bed.CreateAsync(MarkersTestBed.OtherTrip, "Elsewhere");
        _bed.User.SignOut();

        var list = await _bed.Service.ListAsync(MarkersTestBed.Trip, CancellationToken.None);

        Assert.True(list.IsSuccess);
        Assert.Equal(new[] { first.Value.Id, second.Value.Id }, list.Value.Select(m => m.Id));
    }

    [Fact]
    public async Task DeleteAllForTrip_RemovesOnlyThatTripsMarkers()
    {
        await _bed.CreateAsync(MarkersTestBed.Trip);
        await _bed.CreateAsync(MarkersTestBed.Trip);
        await _bed.CreateAsync(MarkersTestBed.OtherTrip);

        var result = await _bed.Service.DeleteAllForTripAsync(MarkersTestBed.Trip, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(_bed.Repository.All, m => Assert.Equal(MarkersTestBed.OtherTrip, m.TripId));
    }
}
