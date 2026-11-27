using TripTracker.Markers.Domain;
using TripTracker.SharedKernel.TripTracker.Markers;

namespace TripTracker.Markers.Application;

internal static class MarkerMapping
{
    public static MarkerDto ToDto(this Marker marker) =>
        new(
            marker.Id,
            marker.TripId,
            marker.Title.Value,
            marker.Description,
            marker.Category,
            marker.Latitude,
            marker.Longitude,
            marker.CreatedAt,
            marker.UpdatedAt);
}
