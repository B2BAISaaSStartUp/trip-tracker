using SixLabors.ImageSharp;
using TripTracker.Photos.Contracts;
using TripTracker.Photos.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Tests.Application;

public class PhotoReadTests
{
    private readonly PhotosTestBed _bed = new();

    private static byte[] GpsJpeg() => TestImages.Jpeg(200, 100, latitude: 50.45, longitude: 30.52);

    private async Task<Guid> UploadAsync(byte[] data, Guid? marker = null)
    {
        var result = await _bed.UploadAsync(data, marker);
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    // ---------- List ----------

    [Fact]
    public async Task List_ReturnsThePhotosOfTheTrip_OldestFirst()
    {
        var first = await UploadAsync(TestImages.Jpeg());
        var second = await UploadAsync(TestImages.Png());
        await _bed.UploadAsync(TestImages.Jpeg(), tripId: PhotosTestBed.OtherTrip);

        var result = await _bed.Service.ListAsync(PhotosTestBed.Trip, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { first, second }, result.Value.Select(p => p.Id).ToArray());
    }

    [Fact]
    public async Task List_CanBeLimitedToOneMarker()
    {
        var onMarker = await UploadAsync(TestImages.Jpeg(), PhotosTestBed.Marker);
        await UploadAsync(TestImages.Jpeg(), PhotosTestBed.OtherMarker);
        await UploadAsync(TestImages.Jpeg());

        var result = await _bed.Service.ListAsync(PhotosTestBed.Trip, PhotosTestBed.Marker, CancellationToken.None);

        Assert.Equal(onMarker, Assert.Single(result.Value).Id);
    }

    [Fact]
    public async Task List_ForATripWithoutPhotos_IsEmpty()
    {
        var result = await _bed.Service.ListAsync(PhotosTestBed.OtherTrip, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task List_ShowsThePositionToTheUploader_ButHidesItFromEveryoneElse()
    {
        await UploadAsync(GpsJpeg());

        var asOwner = (await _bed.Service.ListAsync(PhotosTestBed.Trip, null, CancellationToken.None)).Value.Single();
        _bed.User.SignInAs(PhotosTestBed.Stranger);
        var asStranger = (await _bed.Service.ListAsync(PhotosTestBed.Trip, null, CancellationToken.None)).Value.Single();
        _bed.User.SignOut();
        var asGuest = (await _bed.Service.ListAsync(PhotosTestBed.Trip, null, CancellationToken.None)).Value.Single();

        Assert.NotNull(asOwner.Latitude);
        Assert.NotNull(asOwner.Longitude);
        foreach (var hidden in new[] { asStranger, asGuest })
        {
            Assert.Null(hidden.Latitude);
            Assert.Null(hidden.Longitude);
            Assert.Null(hidden.TakenAt);
            Assert.Equal(asOwner.Id, hidden.Id);
            Assert.Equal(asOwner.Width, hidden.Width);
        }
    }

    // ---------- Open ----------

    [Fact]
    public async Task Open_Original_ByTheUploader_ReturnsTheUntouchedBytes()
    {
        var original = GpsJpeg();
        var id = await UploadAsync(original);

        var result = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Original, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.Value.ContentType);
        Assert.Equal(original, await _bed.ReadAllAsync(result.Value));
    }

    [Fact]
    public async Task Open_Original_ByAnyoneElse_ReturnsTheImageWithoutExifPosition()
    {
        var original = GpsJpeg();
        Assert.True(TestImages.HasGps(original));
        var id = await UploadAsync(original);

        foreach (var viewer in new Guid?[] { PhotosTestBed.Stranger, null })
        {
            if (viewer is { } signedIn)
            {
                _bed.User.SignInAs(signedIn);
            }
            else
            {
                _bed.User.SignOut();
            }

            var result = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Original, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("image/jpeg", result.Value.ContentType);
            var bytes = await _bed.ReadAllAsync(result.Value);
            Assert.False(TestImages.HasGps(bytes));
            var size = Image.Identify(bytes);
            Assert.Equal(200, size.Width);
            Assert.Equal(100, size.Height);
        }
    }

    [Fact]
    public async Task Open_Original_OfAPng_ByAStranger_Works()
    {
        var id = await UploadAsync(TestImages.Png(120, 80));
        _bed.User.SignInAs(PhotosTestBed.Stranger);

        var result = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Original, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(120, Image.Identify(await _bed.ReadAllAsync(result.Value)).Width);
    }

    [Fact]
    public async Task Open_Original_OfAHeic_IsOnlyForTheUploader()
    {
        var heic = TestImages.Heic();
        var id = await UploadAsync(heic);

        var asOwner = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Original, CancellationToken.None);
        _bed.User.SignInAs(PhotosTestBed.Stranger);
        var asStranger = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Original, CancellationToken.None);

        Assert.True(asOwner.IsSuccess);
        Assert.Equal(heic, await _bed.ReadAllAsync(asOwner.Value));
        ErrorAssert.Fails(asStranger, ErrorType.FORBIDDEN, "photo.original.unavailable");
    }

    [Fact]
    public async Task Open_Thumbnail_HasNoExif_ForAnyone()
    {
        var id = await UploadAsync(GpsJpeg());

        var asOwner = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Thumbnail, CancellationToken.None);
        _bed.User.SignOut();
        var asGuest = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Thumbnail, CancellationToken.None);

        foreach (var result in new[] { asOwner, asGuest })
        {
            Assert.True(result.IsSuccess);
            Assert.False(TestImages.HasGps(await _bed.ReadAllAsync(result.Value)));
        }
    }

    [Fact]
    public async Task Open_Thumbnail_OfAHeic_IsNotFound()
    {
        var id = await UploadAsync(TestImages.Heic());

        var result = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Thumbnail, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "photo.thumbnail.unavailable");
    }

    [Theory]
    [InlineData(PhotoVariant.Original)]
    [InlineData(PhotoVariant.Thumbnail)]
    public async Task Open_APhotoThroughTheWrongTrip_IsNotFound(PhotoVariant variant)
    {
        var id = await UploadAsync(TestImages.Jpeg());

        var result = await _bed.Service.OpenAsync(PhotosTestBed.OtherTrip, id, variant, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "photo.not.found");
    }

    [Fact]
    public async Task Open_AnUnknownPhoto_IsNotFound()
    {
        var result = await _bed.Service.OpenAsync(PhotosTestBed.Trip, Guid.NewGuid(), PhotoVariant.Original, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "photo.not.found");
    }

    [Fact]
    public async Task Open_WhenTheFileIsMissingFromStorage_IsNotFound()
    {
        var id = await UploadAsync(TestImages.Jpeg());
        _bed.Store.Remove(_bed.Repository.All[0].OriginalKey);

        var result = await _bed.Service.OpenAsync(PhotosTestBed.Trip, id, PhotoVariant.Original, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "photo.file.missing");
    }
}
