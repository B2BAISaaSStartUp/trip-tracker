using System.Text;

namespace TripTracker.Photos.Domain;

/// <summary>The image formats accepted by FR-16.</summary>
internal enum ImageKind
{
    Jpeg,
    Png,
    Heic,
}

internal static class ImageKinds
{
    public static string ToContentType(this ImageKind kind) =>
        kind switch
        {
            ImageKind.Jpeg => "image/jpeg",
            ImageKind.Png => "image/png",
            _ => "image/heic",
        };

    public static ImageKind FromContentType(string contentType) =>
        contentType switch
        {
            "image/jpeg" => ImageKind.Jpeg,
            "image/png" => ImageKind.Png,
            _ => ImageKind.Heic,
        };

    /// <summary>HEIC cannot be decoded by the imaging library, so it gets no thumbnail and no EXIF reading.</summary>
    public static bool CanDecode(this ImageKind kind) => kind != ImageKind.Heic;
}

/// <summary>Recognises a format from the first bytes of a file, never from its name or declared type.</summary>
internal static class ImageFormatDetector
{
    private static ReadOnlySpan<byte> PngSignature => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static readonly string[] HeicBrands = ["heic", "heix", "heim", "heis", "hevc", "hevx", "mif1", "msf1"];

    public static ImageKind? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ImageKind.Jpeg;
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(PngSignature))
        {
            return ImageKind.Png;
        }

        if (header.Length >= 12 && header.Slice(4, 4).SequenceEqual("ftyp"u8))
        {
            var brand = Encoding.ASCII.GetString(header.Slice(8, 4));
            if (Array.IndexOf(HeicBrands, brand) >= 0)
            {
                return ImageKind.Heic;
            }
        }

        return null;
    }
}
