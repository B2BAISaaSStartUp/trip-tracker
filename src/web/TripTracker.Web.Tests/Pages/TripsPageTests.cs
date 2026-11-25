using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Web.Models;
using TripTracker.Web.Pages;
using TripTracker.Web.Services;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Pages;

public sealed class TripsPageTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();
    private readonly FakeApiClient _api = new();

    public TripsPageTests()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IApiClient>(_api);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // MudBlazor registers services that can only be disposed asynchronously.
    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public void Trips_Loaded_ShowsTitlesAndEnglishDatesForTheNewestFirstPage()
    {
        _api.TripResults.Enqueue(Success(TestTrips.Page(
            2,
            TestTrips.Trip("Lviv", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 9)),
            TestTrips.Trip("Odesa", new DateOnly(2026, 3, 14), new DateOnly(2026, 3, 16)))));

        var page = _context.Render<Trips>();

        var rows = page.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Equal(["Lviv", "1 Jul 2026", "9 Jul 2026"], rows[0].QuerySelectorAll("td").Select(c => c.TextContent.Trim()));
        Assert.DoesNotContain("Showing the newest", page.Markup, StringComparison.Ordinal);
        Assert.Equal(new ListTripsQuery(), Assert.Single(_api.TripQueries));
    }

    [Fact]
    public void Trips_MoreThanOnePage_SaysHowManyAreShown()
    {
        _api.TripResults.Enqueue(Success(TestTrips.Page(
            23,
            TestTrips.Trip("Lviv", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 9)))));

        var page = _context.Render<Trips>();

        Assert.Contains("Showing the newest 1 of 23 trips.", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Trips_None_ShowsTheEmptyState()
    {
        _api.TripResults.Enqueue(Success(TestTrips.Page(0)));

        var page = _context.Render<Trips>();

        Assert.Equal("No trips yet", page.Find("h2").TextContent.Trim());
        Assert.Empty(page.FindAll("table"));
    }

    [Fact]
    public void Trips_ErrorThenRetry_ShowsTheMessageThenTheList()
    {
        _api.TripResults.Enqueue(ApiResult<PagedList<TripDto>>.Failure(
            [new ApiError("client.network.unreachable", "Could not reach the server.", ErrorType.FAILURE)]));
        _api.TripResults.Enqueue(Success(TestTrips.Page(
            1,
            TestTrips.Trip("Lviv", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 9)))));
        var page = _context.Render<Trips>();
        Assert.Contains("Could not reach the server.", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("No trips yet", page.Markup, StringComparison.Ordinal);

        page.FindAll("button").Single(b => b.TextContent.Trim() == "Try again").Click();

        page.WaitForAssertion(() => Assert.Single(page.FindAll("tbody tr")));
        Assert.Empty(page.FindAll("[role=alert]"));
    }

    private static ApiResult<PagedList<TripDto>> Success(PagedList<TripDto> page) =>
        ApiResult<PagedList<TripDto>>.Success(page);
}
