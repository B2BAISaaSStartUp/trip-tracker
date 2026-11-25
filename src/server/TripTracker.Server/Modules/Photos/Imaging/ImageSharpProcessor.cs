using System.Globalization;
using CSharpFunctionalExtensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using TripTracker.Photos.Domain;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Imaging;

/// <summary>ImageSharp implementation of <see cref="IImageProcessor"/>.</summary>
internal sealed class ImageSharpProcessor : IImageProcessor
{
    /// <summary>Longest side of a thumbnail, in pixels.</summary>
    public const int ThumbnailMaxSide = 400;

    /// <summary>
    /// Largest image accepted, in pixels. A small file can unpack to a huge bitmap, so the size is checked
    /// from the header before the image is decoded.
    /// </summary>
    public const long MaxPixels = 100_000_000;

    private const int JpegQuality = 85;

    public Result<ProcessedImage, Error> Process(byte[] data, ImageKind kind)
    {
        if (!kind.CanDecode())
        {
            return new ProcessedImage(ImageFacts.Unknown, null);
        }

        try
        {
            var header = Image.Identify(data);
            if ((long)header.Width * header.Height > MaxPixels)
            {
                return PhotoErrors.TooManyPixels(MaxPixels);
            }

            using var image = Image.Load(data);

            // Read the EXIF data before it is removed for the thumbnail.
            var (latitude, longitude, takenAt) = ReadExif(image.Metadata.ExifProfile);

            image.Mutate(x => x.AutoOrient());
            var facts = new ImageFacts(image.Width, image.Height, latitude, longitude, takenAt);

            if (image.Width > ThumbnailMaxSide || image.Height > ThumbnailMaxSide)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(ThumbnailMaxSide, ThumbnailMaxSide),
                }));
            }

            return new ProcessedImage(facts, EncodeWithoutMetadata(image, kind));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return PhotoErrors.Corrupt();
        }
    }

    public Result<byte[], Error> StripMetadata(byte[] data, ImageKind kind)
    {
        if (!kind.CanDecode())
        {
            return PhotoErrors.OriginalUnavailable();
        }

        try
        {
            using var image = Image.Load(data);

            // Apply the orientation first: once the EXIF block is gone the viewer could no longer rotate it.
            image.Mutate(x => x.AutoOrient());
            return EncodeWithoutMetadata(image, kind);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return PhotoErrors.Corrupt();
        }
    }

    private static byte[] EncodeWithoutMetadata(Image image, ImageKind kind)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IptcProfile = null;

        using var stream = new MemoryStream();
        if (kind == ImageKind.Png)
        {
            image.SaveAsPng(stream);
        }
        else
        {
            image.SaveAsJpeg(stream, new JpegEncoder { Quality = JpegQuality });
        }

        return stream.ToArray();
    }

    private static (double? Latitude, double? Longitude, DateTimeOffset? TakenAt) ReadExif(ExifProfile? exif)
    {
        if (exif is null)
        {
            return (null, null, null);
        }

        var latitude = ReadCoordinate(exif, ExifTag.GPSLatitude, ExifTag.GPSLatitudeRef, "S");
        var longitude = ReadCoordinate(exif, ExifTag.GPSLongitude, ExifTag.GPSLongitudeRef, "W");

        // Keep a position only when both parts are valid.
        if (latitude is not (>= -90 and <= 90) || longitude is not (>= -180 and <= 180))
        {
            latitude = null;
            longitude = null;
        }

        DateTimeOffset? takenAt = null;
        if (exif.TryGetValue(ExifTag.DateTimeOriginal, out IExifValue<string>? original)
            && DateTime.TryParseExact(
                original.Value,
                "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            takenAt = new DateTimeOffset(DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified), TimeSpan.Zero);
        }

        return (latitude, longitude, takenAt);
    }

    /// <summary>Converts degrees, minutes and seconds to decimal degrees.</summary>
    private static double? ReadCoordinate(
        ExifProfile exif,
        ExifTag<Rational[]> valueTag,
        ExifTag<string> referenceTag,
        string negativeReference)
    {
        if (!exif.TryGetValue(valueTag, out IExifValue<Rational[]>? value) || value.Value is not { Length: 3 } parts)
        {
            return null;
        }

        var degrees = parts[0].ToDouble() + (parts[1].ToDouble() / 60) + (parts[2].ToDouble() / 3600);
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
        {
            return null;
        }

        if (exif.TryGetValue(referenceTag, out IExifValue<string>? reference)
            && string.Equals(reference.Value, negativeReference, StringComparison.OrdinalIgnoreCase))
        {
            degrees = -degrees;
        }

        return degrees;
    }
}
