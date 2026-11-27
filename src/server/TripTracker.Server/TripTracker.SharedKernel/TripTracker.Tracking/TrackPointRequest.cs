namespace TripTracker.SharedKernel.TripTracker.Tracking;

/// <summary>One GPS point recorded by the mobile app.</summary>
/// <param name="ClientPointId">
/// Id the app gives the point when it records it, before any upload. Sending the same id again does not store the
/// point twice (NFR-08), so the app can safely resend its whole offline buffer after a lost response.
/// </param>
/// <param name="Latitude">Degrees, -90 to 90.</param>
/// <param name="Longitude">Degrees, -180 to 180.</param>
/// <param name="Altitude">Meters above sea level, if the device reports it.</param>
/// <param name="Accuracy">Horizontal accuracy in meters, if the device reports it.</param>
/// <param name="RecordedAt">When the device recorded the point.</param>
public sealed record TrackPointRequest(
    Guid ClientPointId,
    double Latitude,
    double Longitude,
    double? Altitude,
    double? Accuracy,
    DateTimeOffset RecordedAt);
