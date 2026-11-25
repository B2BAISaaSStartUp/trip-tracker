namespace TripTracker.Photos.Contracts;

/// <summary>Limits from FR-16 and the project constraints. Shared so the API layer and the module agree.</summary>
public static class PhotoLimits
{
    /// <summary>Largest accepted file: 15 MB.</summary>
    public const long MaxFileSizeBytes = 15L * 1024 * 1024;

    /// <summary>Largest request body: the file plus room for the multipart envelope.</summary>
    public const long MaxRequestBytes = MaxFileSizeBytes + 1024 * 1024;

    /// <summary>Most photos on one marker.</summary>
    public const int MaxPhotosPerMarker = 20;

    /// <summary>Storage per user: 2 GB.</summary>
    public const long UserQuotaBytes = 2L * 1024 * 1024 * 1024;
}
