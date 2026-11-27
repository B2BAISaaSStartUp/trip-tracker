namespace TripTracker.SharedKernel.TripTracker.Markers;

/// <summary>Request to put a marker on a trip's map.</summary>
/// <param name="Title">1 to 200 characters.</param>
/// <param name="Description">Optional note, up to 2000 characters.</param>
/// <param name="Category">Kind of place.</param>
/// <param name="Latitude">Degrees, -90 to 90.</param>
/// <param name="Longitude">Degrees, -180 to 180.</param>
public sealed record CreateMarkerRequest(
    string Title,
    string? Description,
    MarkerCategory Category,
    double Latitude,
    double Longitude);
