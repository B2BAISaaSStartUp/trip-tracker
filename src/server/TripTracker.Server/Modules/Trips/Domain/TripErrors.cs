using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Trips.Domain;

/// <summary>Errors produced by the Trips module.</summary>
internal static class TripErrors
{
    public static Error NotFound() =>
        Error.NotFound("trip.not.found", "Trip was not found");

    public static Error NotOwner() =>
        Error.Forbidden("trip.access.forbidden", "You do not have permission to modify this trip");

    public static Error NotAuthenticated() =>
        Error.Forbidden("trip.user.not.authenticated", "You must be signed in to do this");

    public static Error InvalidDates() =>
        Error.Validation("trip.dates.invalid", "EndDate must not be earlier than StartDate", "EndDate");

    public static Error OnlyPublicCanBeHidden() =>
        Error.Conflict("trip.moderation.not.public", "Only public trips can be hidden");
}
