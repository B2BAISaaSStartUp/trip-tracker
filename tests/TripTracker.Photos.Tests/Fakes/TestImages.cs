using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace TripTracker.Photos.Tests.Fakes;

/// <summary>Builds small real images, optionally with an EXIF position, so the imaging code is tested for real.</summary>
internal static class TestImages
{
    public static byte[] Jpeg(
        int width = 64,
        int height = 48,
        double? latitude = null,
        double? longitude = null,
        DateTime? takenAt = null)
    {
        using var image = new Image<Rgba32>(width, height);
        if (latitude is { } lat && longitude is { } lon)
        {
            image.Metadata.ExifProfile = BuildExif(lat, lon, takenAt);
        }

        using var stream = new MemoryStream();
        image.SaveAsJpeg(stream);
        return stream.ToArray();
    }

    public static byte[] Png(int width = 64, int height = 48)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    /// <summary>The first bytes of a HEIC file: a box header, "ftyp" and the "heic" brand.</summary>
    public static byte[] Heic() =>
    [
        0x00, 0x00, 0x00, 0x18,
        (byte)'f', (byte)'t', (byte)'y', (byte)'p',
        (byte)'h', (byte)'e', (byte)'i', (byte)'c',
        0x00, 0x00, 0x00, 0x00,
        (byte)'m', (byte)'i', (byte)'f', (byte)'1',
        (byte)'h', (byte)'e', (byte)'i', (byte)'c',
    ];

    /// <summary>Starts like a JPEG but is not one.</summary>
    public static byte[] CorruptJpeg()
    {
        var bytes = new byte[200];
        new Random(7).NextBytes(bytes);
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return bytes;
    }

    public static bool HasGps(byte[] imageBytes)
    {
        using var image = Image.Load(imageBytes);
        var exif = image.Metadata.ExifProfile;
        return exif is not null && exif.TryGetValue(ExifTag.GPSLatitude, out IExifValue<Rational[]>? latitude) && latitude is not null;
    }

    private static ExifProfile BuildExif(double latitude, double longitude, DateTime? takenAt)
    {
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.GPSLatitude, ToDegreesMinutesSeconds(Math.Abs(latitude)));
        exif.SetValue(ExifTag.GPSLatitudeRef, latitude >= 0 ? "N" : "S");
        exif.SetValue(ExifTag.GPSLongitude, ToDegreesMinutesSeconds(Math.Abs(longitude)));
        exif.SetValue(ExifTag.GPSLongitudeRef, longitude >= 0 ? "E" : "W");

        if (takenAt is { } taken)
        {
            exif.SetValue(ExifTag.DateTimeOriginal, taken.ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        return exif;
    }

    private static Rational[] ToDegreesMinutesSeconds(double degrees)
    {
        var whole = Math.Floor(degrees);
        var minutesFull = (degrees - whole) * 60;
        var minutes = Math.Floor(minutesFull);
        var seconds = (minutesFull - minutes) * 60;

        return
        [
            new Rational((uint)whole, 1),
            new Rational((uint)minutes, 1),
            new Rational((uint)Math.Round(seconds * 100), 100),
        ];
    }
}
