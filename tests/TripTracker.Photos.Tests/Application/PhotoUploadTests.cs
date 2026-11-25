using SixLabors.ImageSharp;
using TripTracker.Photos.Contracts;
using TripTracker.Photos.Domain;
using TripTracker.Photos.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Tests.Application;

public class PhotoUploadTests
{
    private readonly PhotosTestBed _bed = new();

    [Fact]
    public async Task Upload_Jpeg_StoresTheOriginalAndAThumbnail()
    {
        var jpeg = TestImages.Jpeg(1000, 500);

        var result = await _bed.UploadAsync(jpeg, PhotosTestBed.Marker, "holiday.jpg");

        Assert.True(result.IsSuccess);
        var photo = result.Value;
        Assert.Equal(PhotosTestBed.Trip, photo.TripId);
        Assert.Equal(PhotosTestBed.Marker, photo.MarkerId);
        Assert.Equal("holiday.jpg", photo.FileName);
        Assert.Equal("image/jpeg", photo.ContentType);
        Assert.Equal(jpeg.Length, photo.SizeBytes);
        Assert.Equal(1000, photo.Width);
        Assert.Equal(500, photo.Height);
        Assert.True(photo.HasThumbnail);

        var stored = Assert.Single(_bed.Repository.All);
        Assert.Equal(PhotosTestBed.Owner, stored.OwnerId);
        Assert.Equal(jpeg, _bed.Store.Read(stored.OriginalKey));
        Assert.Equal(2, _bed.Store.Keys.Count);
        Assert.Equal("image/jpeg", _bed.Store.ContentTypeOf(stored.ThumbnailKey!));
    }

    [Fact]
    public async Task Upload_MakesAThumbnailNoLargerThanTheLimit_KeepingTheProportions()
    {
        await _bed.UploadAsync(TestImages.Jpeg(1000, 500));

        var thumbnail = _bed.Store.Read(_bed.Repository.All[0].ThumbnailKey!);
        var size = Image.Identify(thumbnail);

        Assert.Equal(400, size.Width);
        Assert.Equal(200, size.Height);
    }

    [Fact]
    public async Task Upload_SmallImage_ThumbnailIsNotEnlarged()
    {
        await _bed.UploadAsync(TestImages.Jpeg(64, 48));

        var size = Image.Identify(_bed.Store.Read(_bed.Repository.All[0].ThumbnailKey!));

        Assert.Equal(64, size.Width);
        Assert.Equal(48, size.Height);
    }

    [Fact]
    public async Task Upload_Png_Works()
    {
        var png = TestImages.Png(800, 600);

        var result = await _bed.UploadAsync(png, fileName: "map.png");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.True(result.Value.HasThumbnail);
        Assert.Equal("image/png", _bed.Store.ContentTypeOf(_bed.Repository.All[0].ThumbnailKey!));
    }

    [Fact]
    public async Task Upload_Heic_IsStoredWithoutAThumbnailOrDimensions()
    {
        var heic = TestImages.Heic();

        var result = await _bed.UploadAsync(heic, fileName: "IMG_0001.HEIC");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/heic", result.Value.ContentType);
        Assert.False(result.Value.HasThumbnail);
        Assert.Null(result.Value.Width);
        Assert.Null(result.Value.Height);
        Assert.Null(result.Value.Latitude);
        Assert.Single(_bed.Store.Keys);
    }

    [Fact]
    public async Task Upload_ReadsTheFormatFromTheBytes_NotFromTheFileName()
    {
        var result = await _bed.UploadAsync(TestImages.Png(), fileName: "really-a-png.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/png", result.Value.ContentType);
    }

    [Fact]
    public async Task Upload_ReadsTheGpsPosition_AndCaptureTime_FromExif()
    {
        var jpeg = TestImages.Jpeg(latitude: 50.4501, longitude: 30.5234, takenAt: new DateTime(2026, 7, 4, 12, 30, 15));

        var result = await _bed.UploadAsync(jpeg);

        Assert.True(result.IsSuccess);
        Assert.Equal(50.4501, result.Value.Latitude!.Value, 3);
        Assert.Equal(30.5234, result.Value.Longitude!.Value, 3);
        Assert.Equal(new DateTimeOffset(2026, 7, 4, 12, 30, 15, TimeSpan.Zero), result.Value.TakenAt);
    }

    [Fact]
    public async Task Upload_ReadsSouthernAndWesternHemispheresAsNegative()
    {
        var jpeg = TestImages.Jpeg(latitude: -33.8688, longitude: -70.6693);

        var result = await _bed.UploadAsync(jpeg);

        Assert.Equal(-33.8688, result.Value.Latitude!.Value, 3);
        Assert.Equal(-70.6693, result.Value.Longitude!.Value, 3);
    }

    [Fact]
    public async Task Upload_WithoutExif_HasNoPosition()
    {
        var result = await _bed.UploadAsync(TestImages.Jpeg());

        Assert.Null(result.Value.Latitude);
        Assert.Null(result.Value.Longitude);
        Assert.Null(result.Value.TakenAt);
    }

    [Fact]
    public async Task Upload_WhenNotSignedIn_IsRejected_AndStoresNothing()
    {
        _bed.User.SignOut();

        var result = await _bed.UploadAsync(TestImages.Jpeg());

        ErrorAssert.Fails(result, ErrorType.UNAUTHORIZED, "photo.user.not.authenticated");
        Assert.Empty(_bed.Store.Keys);
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Upload_OfATextFile_IsRejectedAsUnsupported()
    {
        var result = await _bed.UploadAsync("just some text, not an image"u8.ToArray(), fileName: "photo.jpg");

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "photo.format.unsupported");
        Assert.Empty(_bed.Store.Keys);
    }

    [Fact]
    public async Task Upload_OfACorruptJpeg_IsRejected_AndStoresNothing()
    {
        var result = await _bed.UploadAsync(TestImages.CorruptJpeg());

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "photo.file.corrupt");
        Assert.Empty(_bed.Store.Keys);
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Upload_OfAnEmptyFile_IsRejected()
    {
        var result = await _bed.UploadAsync([]);

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "photo.file.required");
    }

    [Fact]
    public async Task Upload_LargerThanTheLimit_ByDeclaredLength_IsRejected()
    {
        var request = new PhotoUploadRequest(new MemoryStream([1]), "big.jpg", PhotoLimits.MaxFileSizeBytes + 1, null);

        var result = await _bed.Service.UploadAsync(PhotosTestBed.Trip, request, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "photo.file.too.large");
    }

    [Fact]
    public async Task Upload_LargerThanTheLimit_EvenWhenTheDeclaredLengthLies_IsRejected()
    {
        var content = new MemoryStream(new byte[PhotoLimits.MaxFileSizeBytes + 1]);
        var request = new PhotoUploadRequest(content, "big.jpg", 10, null);

        var result = await _bed.Service.UploadAsync(PhotosTestBed.Trip, request, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "photo.file.too.large");
        Assert.Empty(_bed.Store.Keys);
    }

    [Fact]
    public async Task Upload_AMarkerAcceptsTwentyPhotos_ButNotTwentyOne()
    {
        for (var i = 0; i < PhotoLimits.MaxPhotosPerMarker; i++)
        {
            _bed.Seed(PhotosTestBed.Marker);
        }

        var twentyFirst = await _bed.UploadAsync(TestImages.Jpeg(), PhotosTestBed.Marker);
        var otherMarker = await _bed.UploadAsync(TestImages.Jpeg(), PhotosTestBed.OtherMarker);
        var noMarker = await _bed.UploadAsync(TestImages.Jpeg());

        ErrorAssert.Fails(twentyFirst, ErrorType.CONFLICT, "photo.marker.limit");
        Assert.True(otherMarker.IsSuccess);
        Assert.True(noMarker.IsSuccess);
    }

    [Fact]
    public async Task Upload_TheMarkerLimitIsPerTrip()
    {
        for (var i = 0; i < PhotoLimits.MaxPhotosPerMarker; i++)
        {
            _bed.Seed(PhotosTestBed.Marker, tripId: PhotosTestBed.OtherTrip);
        }

        var result = await _bed.UploadAsync(TestImages.Jpeg(), PhotosTestBed.Marker);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Upload_BeyondTheUserQuota_IsRejected()
    {
        _bed.Seed(sizeBytes: PhotoLimits.UserQuotaBytes - 5);

        var result = await _bed.UploadAsync(TestImages.Jpeg());

        ErrorAssert.Fails(result, ErrorType.CONFLICT, "photo.quota.exceeded");
        Assert.Empty(_bed.Store.Keys);
    }

    [Fact]
    public async Task Upload_TheQuotaIsPerUser()
    {
        _bed.Seed(sizeBytes: PhotoLimits.UserQuotaBytes, ownerId: PhotosTestBed.Stranger);

        var result = await _bed.UploadAsync(TestImages.Jpeg());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Upload_WhenTheDatabaseFails_RemovesTheStoredFiles()
    {
        _bed.Repository.FailOnAdd = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _bed.UploadAsync(TestImages.Jpeg(1000, 500)));

        Assert.Empty(_bed.Store.Keys);
    }

    [Theory]
    [InlineData("C:\\Users\\me\\Pictures\\a.jpg", "a.jpg")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("  spaced name.png  ", "spaced name.png")]
    [InlineData("", "photo.jpg")]
    [InlineData("   ", "photo.jpg")]
    public void CleanFileName_DropsPathsAndFillsGaps(string input, string expected)
    {
        Assert.Equal(expected, Photo.CleanFileName(input, ImageKind.Jpeg));
    }

    [Fact]
    public void CleanFileName_ShortensVeryLongNames()
    {
        var cleaned = Photo.CleanFileName(new string('a', 500) + ".jpg", ImageKind.Jpeg);

        Assert.Equal(Photo.FileNameMaxLength, cleaned.Length);
    }
}
