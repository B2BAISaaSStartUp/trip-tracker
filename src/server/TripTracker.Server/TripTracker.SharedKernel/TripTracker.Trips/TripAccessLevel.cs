using System.Text.Json.Serialization;

namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Who may see a trip (FR-19).</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TripAccessLevel
{
    /// <summary>Only the owner.</summary>
    Private = 0,

    /// <summary>Anyone who knows the share token.</summary>
    ByLink = 1,

    /// <summary>Anyone.</summary>
    Public = 2,
}
