namespace TripTracker.SharedKernel.TripTracker.Tracking;

/// <summary>One point of a trip's route, as shown on the map.</summary>
public sealed record TrackPointDto(
    Guid ClientPointId,
    double Latitude,
    double Longitude,
    double? Altitude,
    double? Accuracy,
    DateTimeOffset RecordedAt);
