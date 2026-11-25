using TripTracker.Photos.Domain;
using TripTracker.Photos.Tests.Fakes;

namespace TripTracker.Photos.Tests.Domain;

public class ImageFormatDetectorTests
{
    [Fact]
    public void Detect_RecognisesJpeg() => Assert.Equal(ImageKind.Jpeg, ImageFormatDetector.Detect(TestImages.Jpeg()));

    [Fact]
    public void Detect_RecognisesPng() => Assert.Equal(ImageKind.Png, ImageFormatDetector.Detect(TestImages.Png()));

    [Fact]
    public void Detect_RecognisesHeic() => Assert.Equal(ImageKind.Heic, ImageFormatDetector.Detect(TestImages.Heic()));

    [Theory]
    [InlineData("heic")]
    [InlineData("heix")]
    [InlineData("mif1")]
    [InlineData("msf1")]
    [InlineData("hevc")]
    public void Detect_RecognisesTheUsualHeicBrands(string brand)
    {
        var bytes = new byte[16];
        "ftyp"u8.CopyTo(bytes.AsSpan(4));
        System.Text.Encoding.ASCII.GetBytes(brand).CopyTo(bytes, 8);

        Assert.Equal(ImageKind.Heic, ImageFormatDetector.Detect(bytes));
    }

    [Theory]
    [InlineData("avif")]
    [InlineData("mp42")]
    [InlineData("isom")]
    public void Detect_DoesNotMistakeOtherMediaBoxesForHeic(string brand)
    {
        var bytes = new byte[16];
        "ftyp"u8.CopyTo(bytes.AsSpan(4));
        System.Text.Encoding.ASCII.GetBytes(brand).CopyTo(bytes, 8);

        Assert.Null(ImageFormatDetector.Detect(bytes));
    }

    [Fact]
    public void Detect_RejectsGif() => Assert.Null(ImageFormatDetector.Detect("GIF89a......"u8));

    [Fact]
    public void Detect_RejectsPlainText() => Assert.Null(ImageFormatDetector.Detect("hello world, not an image"u8));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(11)]
    public void Detect_HandlesInputThatIsTooShort(int length)
    {
        // Only a few bytes of a real file: must not throw, whatever the length.
        var bytes = TestImages.Heic().AsSpan(0, length);

        Assert.Null(ImageFormatDetector.Detect(bytes));
    }

    [Fact]
    public void ContentTypes_RoundTrip()
    {
        foreach (var kind in Enum.GetValues<ImageKind>())
        {
            Assert.Equal(kind, ImageKinds.FromContentType(kind.ToContentType()));
        }
    }
}
