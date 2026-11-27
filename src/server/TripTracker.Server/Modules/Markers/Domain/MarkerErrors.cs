using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Domain;

/// <summary>Errors produced by the Markers module.</summary>
internal static class MarkerErrors
{
    public static Error NotFound() =>
        Error.NotFound("marker.not.found", "Marker was not found");

    public static Error NotAuthenticated() =>
        Error.Forbidden("marker.user.not.authenticated", "You must be signed in to do this");

    public static Error InvalidLatitude() =>
        Error.Validation("marker.latitude.invalid", "Latitude must be between -90 and 90", "Latitude");

    public static Error InvalidLongitude() =>
        Error.Validation("marker.longitude.invalid", "Longitude must be between -180 and 180", "Longitude");
}
