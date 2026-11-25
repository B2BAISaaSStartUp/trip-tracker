using CSharpFunctionalExtensions;
using TripTracker.Photos.Domain;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Imaging;

/// <summary>What is known about an image after reading it. Everything is <c>null</c> when it could not be read.</summary>
/// <param name="Width">Width after applying the EXIF orientation.</param>
/// <param name="Height">Height after applying the EXIF orientation.</param>
/// <param name="Latitude">Decimal degrees from EXIF GPS, positive north.</param>
/// <param name="Longitude">Decimal degrees from EXIF GPS, positive east.</param>
/// <param name="TakenAt">EXIF capture time (wall-clock time, no zone; stored with offset zero).</param>
internal sealed record ImageFacts(int? Width, int? Height, double? Latitude, double? Longitude, DateTimeOffset? TakenAt)
{
    public static readonly ImageFacts Unknown = new(null, null, null, null, null);
}

/// <summary>The facts about an image and its thumbnail; the thumbnail is <c>null</c> for formats that cannot be decoded.</summary>
internal sealed record ProcessedImage(ImageFacts Facts, byte[]? Thumbnail);

/// <summary>Reads images and produces thumbnails. Hides the imaging library from the rest of the module.</summary>
internal interface IImageProcessor
{
    /// <summary>
    /// Reads size, orientation and EXIF data and makes a thumbnail without metadata. HEIC is accepted as is,
    /// with unknown facts and no thumbnail. Fails with a validation error for a corrupt or oversized image.
    /// </summary>
    Result<ProcessedImage, Error> Process(byte[] data, ImageKind kind);

    /// <summary>Returns a copy of the image with all EXIF, XMP and IPTC metadata removed and the orientation applied.</summary>
    Result<byte[], Error> StripMetadata(byte[] data, ImageKind kind);
}
