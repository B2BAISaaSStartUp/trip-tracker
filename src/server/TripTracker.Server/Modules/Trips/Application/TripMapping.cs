using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Application;

internal static class TripMapping
{
    /// <summary>Maps a trip for an authenticated viewer; the share token is included only for the owner.</summary>
    public static TripDto ToDto(this Trip trip, bool includeShareToken) =>
        new(
            trip.Id,
            trip.OwnerId,
            trip.Title.Value,
            trip.Description,
            trip.StartDate,
            trip.EndDate,
            trip.AccessLevel,
            includeShareToken ? trip.ShareToken : null,
            trip.IsHidden,
            trip.CreatedAt,
            trip.UpdatedAt);

    public static SharedTripDto ToSharedDto(this Trip trip) =>
        new(
            trip.Id,
            trip.Title.Value,
            trip.Description,
            trip.StartDate,
            trip.EndDate,
            trip.AccessLevel);
}
