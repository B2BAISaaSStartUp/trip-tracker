using TripTracker.SharedKernel.TripTracker.Tracking;
using TripTracker.Tracking.Domain;

namespace TripTracker.Tracking.Application;

internal static class TrackPointMapping
{
    public static TrackPointDto ToDto(this TrackPoint point) =>
        new(
            point.ClientPointId,
            point.Latitude,
            point.Longitude,
            point.Altitude,
            point.Accuracy,
            point.RecordedAt);
}
