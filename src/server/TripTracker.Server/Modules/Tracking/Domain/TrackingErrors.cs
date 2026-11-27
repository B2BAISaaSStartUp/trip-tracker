using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Tracking.Domain;

/// <summary>Errors produced by the Tracking module.</summary>
internal static class TrackingErrors
{
    public static Error NotAuthenticated() =>
        Error.Forbidden("tracking.user.not.authenticated", "You must be signed in to do this");

    public static Error EmptyBatch() =>
        Error.Validation("tracking.batch.empty", "At least one point is required", "Points");

    public static Error BatchTooLarge(int max) =>
        Error.Validation("tracking.batch.too.large", $"A batch must not contain more than {max} points", "Points");

    public static Error InvalidLatitude() =>
        Error.Validation("tracking.point.latitude.invalid", "Latitude must be between -90 and 90", "Latitude");

    public static Error InvalidLongitude() =>
        Error.Validation("tracking.point.longitude.invalid", "Longitude must be between -180 and 180", "Longitude");

    public static Error RecordedInFuture() =>
        Error.Validation("tracking.point.time.in.future", "RecordedAt must not be in the future", "RecordedAt");
}
