using SixLabors.ImageSharp;
using TripTracker.Photos.Domain;
using TripTracker.Photos.Imaging;
using TripTracker.Photos.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Tests.Imaging;

public class ImageSharpProcessorTests
{
    private readonly ImageSharpProcessor _processor = new();

    [Fact]
    public void Process_Heic_ReturnsUnknownFactsAndNoThumbnail()
    {
        var result = _processor.Process(TestImages.Heic(), ImageKind.Heic);

        Assert.True(result.IsSuccess);
        Assert.Equal(ImageFacts.Unknown, result.Value.Facts);
        Assert.Null(result.Value.Thumbnail);
    }

    [Fact]
    public void Process_CorruptImage_IsAValidationError()
    {
        var result = _processor.Process(TestImages.CorruptJpeg(), ImageKind.Jpeg);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Equal("photo.file.corrupt", result.Error.Code);
    }

    [Fact]
    public void Process_ThumbnailOfAPortraitPhoto_FitsInTheBox()
    {
        var result = _processor.Process(TestImages.Jpeg(300, 900), ImageKind.Jpeg);

        var size = Image.Identify(result.Value.Thumbnail!);
        Assert.InRange(size.Width, 132, 134);
        Assert.Equal(400, size.Height);
    }

    [Fact]
    public void StripMetadata_RemovesGps_AndKeepsThePicture()
    {
        var original = TestImages.Jpeg(120, 60, latitude: 10.5, longitude: 20.5);

        var result = _processor.StripMetadata(original, ImageKind.Jpeg);

        Assert.True(result.IsSuccess);
        Assert.False(TestImages.HasGps(result.Value));
        var size = Image.Identify(result.Value);
        Assert.Equal(120, size.Width);
        Assert.Equal(60, size.Height);
    }

    [Fact]
    public void StripMetadata_OfHeic_IsRefused()
    {
        var result = _processor.StripMetadata(TestImages.Heic(), ImageKind.Heic);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.FORBIDDEN, result.Error.Type);
    }

    [Fact]
    public void StripMetadata_OfCorruptData_IsAValidationError()
    {
        var result = _processor.StripMetadata(TestImages.CorruptJpeg(), ImageKind.Jpeg);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
    }
}
