namespace TripTracker.Photos.Contracts;

/// <summary>Which file of a photo to read.</summary>
public enum PhotoVariant
{
    /// <summary>The uploaded file.</summary>
    Original,

    /// <summary>The small preview used in the gallery and on the map.</summary>
    Thumbnail,
}
