namespace TripTracker.SharedKernel.TripTracker.Markers;

/// <summary>A marker on a trip's map.</summary>
public sealed record MarkerDto(
    Guid Id,
    Guid TripId,
    string Title,
    string? Description,
    MarkerCategory Category,
    double Latitude,
    double Longitude,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
