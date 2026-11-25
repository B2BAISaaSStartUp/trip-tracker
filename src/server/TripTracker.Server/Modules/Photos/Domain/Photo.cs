using TripTracker.Photos.Imaging;

namespace TripTracker.Photos.Domain;

/// <summary>Metadata of one stored photo. The bytes live in the object store under <see cref="OriginalKey"/>.</summary>
internal sealed class Photo
{
    public const int FileNameMaxLength = 255;
    public const int ContentTypeMaxLength = 32;
    public const int KeyMaxLength = 128;

    // Required by EF Core.
    private Photo()
    {
        FileName = null!;
        ContentType = null!;
        OriginalKey = null!;
    }

    public Guid Id { get; private set; }

    public Guid TripId { get; private set; }

    public Guid? MarkerId { get; private set; }

    /// <summary>The user who uploaded the photo; only this user may change or delete it.</summary>
    public Guid OwnerId { get; private set; }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long SizeBytes { get; private set; }

    public string OriginalKey { get; private set; }

    public string? ThumbnailKey { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public DateTimeOffset? TakenAt { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    public static Photo Create(
        Guid tripId,
        Guid? markerId,
        Guid ownerId,
        string fileName,
        ImageKind kind,
        long sizeBytes,
        ImageFacts facts,
        bool hasThumbnail,
        DateTimeOffset now)
    {
        var id = Guid.NewGuid();
        return new Photo
        {
            Id = id,
            TripId = tripId,
            MarkerId = markerId,
            OwnerId = ownerId,
            FileName = CleanFileName(fileName, kind),
            ContentType = kind.ToContentType(),
            SizeBytes = sizeBytes,
            OriginalKey = $"{tripId}/{id}/original",
            ThumbnailKey = hasThumbnail ? $"{tripId}/{id}/thumbnail" : null,
            Width = facts.Width,
            Height = facts.Height,
            Latitude = facts.Latitude,
            Longitude = facts.Longitude,
            TakenAt = facts.TakenAt,
            UploadedAt = now,
        };
    }

    public void AssignMarker(Guid? markerId) => MarkerId = markerId;

    /// <summary>Drops any path the client sent, trims and shortens the name, and falls back to a neutral one.</summary>
    internal static string CleanFileName(string? fileName, ImageKind kind)
    {
        var name = string.IsNullOrWhiteSpace(fileName)
            ? string.Empty
            : fileName.Replace('\\', '/').Split('/')[^1].Trim();

        if (name.Length == 0)
        {
            name = kind switch
            {
                ImageKind.Jpeg => "photo.jpg",
                ImageKind.Png => "photo.png",
                _ => "photo.heic",
            };
        }

        return name.Length <= FileNameMaxLength ? name : name[..FileNameMaxLength];
    }
}
