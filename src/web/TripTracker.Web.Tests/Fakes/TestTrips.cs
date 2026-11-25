using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Trips;

namespace TripTracker.Web.Tests.Fakes;

internal static class TestTrips
{
    public static TripDto Trip(string title, DateOnly start, DateOnly end) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        title,
        null,
        start,
        end,
        TripAccessLevel.ByLink,
        "share-token",
        false,
        new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero));

    public static PagedList<TripDto> Page(long totalCount, params TripDto[] trips) =>
        new() { Items = trips, TotalCount = totalCount, Page = 1, PageSize = 20 };
}
