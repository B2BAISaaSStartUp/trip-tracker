using System.Text.Json.Serialization;

namespace TripTracker.SharedKernel.TripTracker.Markers;

/// <summary>What kind of place a marker points at. The clients choose the icon by it.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MarkerCategory
{
    /// <summary>Anything that fits no other category.</summary>
    Other = 0,

    /// <summary>A sight, museum or landmark.</summary>
    Sight = 1,

    /// <summary>A restaurant, cafe or bar.</summary>
    Food = 2,

    /// <summary>A hotel, hostel or campsite.</summary>
    Accommodation = 3,

    /// <summary>A viewpoint, beach, park or trail.</summary>
    Nature = 4,

    /// <summary>A station, airport, port or parking.</summary>
    Transport = 5,
}
