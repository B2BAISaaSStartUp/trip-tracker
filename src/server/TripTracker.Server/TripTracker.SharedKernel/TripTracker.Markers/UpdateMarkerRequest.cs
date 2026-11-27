namespace TripTracker.SharedKernel.TripTracker.Markers;

/// <summary>Request to change a marker. Every field is replaced, as in <see cref="CreateMarkerRequest"/>.</summary>
public sealed record UpdateMarkerRequest(
    string Title,
    string? Description,
    MarkerCategory Category,
    double Latitude,
    double Longitude);
