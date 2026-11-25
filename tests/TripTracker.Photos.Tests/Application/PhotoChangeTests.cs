using TripTracker.Photos.Contracts;
using TripTracker.Photos.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Tests.Application;

public class PhotoChangeTests
{
    private readonly PhotosTestBed _bed = new();

    private async Task<Guid> UploadAsync(Guid? marker = null)
    {
        var result = await _bed.UploadAsync(TestImages.Jpeg(1000, 500), marker);
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_RemovesTheRecordAndBothFiles()
    {
        var id = await UploadAsync();

        var result = await _bed.Service.DeleteAsync(PhotosTestBed.Trip, id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_bed.Repository.All);
        Assert.Empty(_bed.Store.Keys);
    }

    [Fact]
    public async Task Delete_ByAnotherUser_IsForbidden_AndKeepsEverything()
    {
        var id = await UploadAsync();
        _bed.User.SignInAs(PhotosTestBed.Stranger);

        var result = await _bed.Service.DeleteAsync(PhotosTestBed.Trip, id, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.FORBIDDEN, "photo.access.forbidden");
        Assert.Single(_bed.Repository.All);
        Assert.Equal(2, _bed.Store.Keys.Count);
    }

    [Fact]
    public async Task Delete_WhenNotSignedIn_IsUnauthorized()
    {
        var id = await UploadAsync();
        _bed.User.SignOut();

        var result = await _bed.Service.DeleteAsync(PhotosTestBed.Trip, id, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.UNAUTHORIZED, "photo.user.not.authenticated");
    }

    [Fact]
    public async Task Delete_ThroughTheWrongTrip_IsNotFound()
    {
        var id = await UploadAsync();

        var result = await _bed.Service.DeleteAsync(PhotosTestBed.OtherTrip, id, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "photo.not.found");
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task Delete_WhenTheStorageIsDown_StillRemovesTheRecord()
    {
        var id = await UploadAsync();
        _bed.Store.FailOnDelete = true;

        var result = await _bed.Service.DeleteAsync(PhotosTestBed.Trip, id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_bed.Repository.All);
    }

    // ---------- Assign marker ----------

    [Fact]
    public async Task AssignMarker_AttachesAndDetachesAPhoto()
    {
        var id = await UploadAsync();

        var attached = await _bed.Service.AssignMarkerAsync(PhotosTestBed.Trip, id, PhotosTestBed.Marker, CancellationToken.None);
        var detached = await _bed.Service.AssignMarkerAsync(PhotosTestBed.Trip, id, null, CancellationToken.None);

        Assert.Equal(PhotosTestBed.Marker, attached.Value.MarkerId);
        Assert.Null(detached.Value.MarkerId);
        Assert.Null(_bed.Repository.All[0].MarkerId);
    }

    [Fact]
    public async Task AssignMarker_StopsAtTwentyPhotos()
    {
        for (var i = 0; i < PhotoLimits.MaxPhotosPerMarker; i++)
        {
            _bed.Seed(PhotosTestBed.Marker);
        }

        var id = await UploadAsync();

        var result = await _bed.Service.AssignMarkerAsync(PhotosTestBed.Trip, id, PhotosTestBed.Marker, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.CONFLICT, "photo.marker.limit");
        Assert.Null(_bed.Repository.All.Single(p => p.Id == id).MarkerId);
    }

    [Fact]
    public async Task AssignMarker_ToTheMarkerItIsAlreadyOn_IsAllowedEvenWhenFull()
    {
        var id = await UploadAsync(PhotosTestBed.Marker);
        for (var i = 1; i < PhotoLimits.MaxPhotosPerMarker; i++)
        {
            _bed.Seed(PhotosTestBed.Marker);
        }

        var result = await _bed.Service.AssignMarkerAsync(PhotosTestBed.Trip, id, PhotosTestBed.Marker, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignMarker_ByAnotherUser_IsForbidden()
    {
        var id = await UploadAsync();
        _bed.User.SignInAs(PhotosTestBed.Stranger);

        var result = await _bed.Service.AssignMarkerAsync(PhotosTestBed.Trip, id, PhotosTestBed.Marker, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.FORBIDDEN, "photo.access.forbidden");
    }

    // ---------- Cleanup when a trip is deleted ----------

    [Fact]
    public async Task DeleteAllForTrip_RemovesThatTripsPhotosAndFiles_AndNothingElse()
    {
        await UploadAsync();
        await UploadAsync(PhotosTestBed.Marker);
        var otherTripPhoto = await _bed.UploadAsync(TestImages.Jpeg(), tripId: PhotosTestBed.OtherTrip);

        var result = await _bed.Service.DeleteAllForTripAsync(PhotosTestBed.Trip, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var remaining = Assert.Single(_bed.Repository.All);
        Assert.Equal(otherTripPhoto.Value.Id, remaining.Id);
        Assert.Contains(remaining.OriginalKey, _bed.Store.Keys);
        Assert.Equal(2, _bed.Store.Keys.Count);
    }

    [Fact]
    public async Task DeleteAllForTrip_WithNoPhotos_Succeeds()
    {
        var result = await _bed.Service.DeleteAllForTripAsync(PhotosTestBed.OtherTrip, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAllForTrip_WhenTheStorageIsDown_Fails_AndKeepsTheRecords()
    {
        await UploadAsync();
        _bed.Store.FailOnDelete = true;

        var result = await _bed.Service.DeleteAllForTripAsync(PhotosTestBed.Trip, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.FAILURE, "photo.storage.unavailable");
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task DeleteAllForTrip_DoesNotNeedASignedInUser()
    {
        await UploadAsync();
        _bed.User.SignOut();

        var result = await _bed.Service.DeleteAllForTripAsync(PhotosTestBed.Trip, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_bed.Repository.All);
    }
}
