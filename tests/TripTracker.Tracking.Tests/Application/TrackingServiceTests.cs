using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Tracking;
using TripTracker.Tracking.Tests.Fakes;

namespace TripTracker.Tracking.Tests.Application;

public class TrackingServiceTests
{
    private readonly TrackingTestBed _bed = new();

    [Fact]
    public async Task Upload_StoresEveryNewPoint()
    {
        var result = await _bed.UploadAsync(TrackingTestBed.Point(0), TrackingTestBed.Point(1), TrackingTestBed.Point(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(new TrackUploadResultDto(3, 3, 0), result.Value);
        Assert.Equal(3, _bed.Repository.All.Count);
        Assert.All(_bed.Repository.All, p => Assert.Equal(TrackingTestBed.Owner, p.RecordedBy));
    }

    // NFR-08: the app resends its buffer when the answer to the first upload is lost.
    [Fact]
    public async Task Upload_ResentPoint_IsNotStoredTwice()
    {
        var point = TrackingTestBed.Point();

        var first = await _bed.UploadAsync(point);
        var second = await _bed.UploadAsync(point);

        Assert.Equal(new TrackUploadResultDto(1, 1, 0), first.Value);
        Assert.Equal(new TrackUploadResultDto(1, 0, 1), second.Value);
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task Upload_BatchOverlappingAStoredOne_StoresOnlyTheNewPoints()
    {
        var a = TrackingTestBed.Point(0);
        var b = TrackingTestBed.Point(1);
        var c = TrackingTestBed.Point(2);
        await _bed.UploadAsync(a, b);

        var result = await _bed.UploadAsync(b, c);

        Assert.Equal(new TrackUploadResultDto(2, 1, 1), result.Value);
        Assert.Equal(3, _bed.Repository.All.Count);
    }

    [Fact]
    public async Task Upload_SamePointTwiceInOneBatch_IsStoredOnce()
    {
        var point = TrackingTestBed.Point();

        var result = await _bed.UploadAsync(point, point);

        Assert.Equal(new TrackUploadResultDto(2, 1, 1), result.Value);
        Assert.Single(_bed.Repository.All);
    }

    [Fact]
    public async Task Upload_SameClientPointIdInAnotherTrip_IsStored()
    {
        var id = Guid.NewGuid();
        await _bed.UploadAsync(TrackingTestBed.Trip, TrackingTestBed.Point(clientPointId: id));

        var result = await _bed.UploadAsync(TrackingTestBed.OtherTrip, TrackingTestBed.Point(clientPointId: id));

        Assert.Equal(1, result.Value.Stored);
        Assert.Equal(2, _bed.Repository.All.Count);
    }

    [Fact]
    public async Task Upload_WithOneInvalidPoint_StoresNothingAndNamesThePoint()
    {
        var result = await _bed.UploadAsync(TrackingTestBed.Point(0), TrackingTestBed.Point(1, latitude: 91));

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "tracking.point.latitude.invalid");
        Assert.Equal("Points[1].Latitude", result.Error.InvalidField);
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Upload_EmptyBatch_IsRejected()
    {
        var result = await _bed.UploadAsync();

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "tracking.batch.empty");
    }

    [Fact]
    public async Task Upload_TooLargeBatch_IsRejected()
    {
        var points = Enumerable.Range(0, UploadTrackPointsRequest.MaxPoints + 1)
            .Select(i => TrackingTestBed.Point(i))
            .ToArray();

        var result = await _bed.UploadAsync(points);

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "tracking.batch.too.large");
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task Upload_WhenNotSignedIn_IsForbidden()
    {
        _bed.User.SignOut();

        var result = await _bed.UploadAsync(TrackingTestBed.Point());

        ErrorAssert.Fails(result, ErrorType.FORBIDDEN, "tracking.user.not.authenticated");
        Assert.Empty(_bed.Repository.All);
    }

    [Fact]
    public async Task GetRoute_ReturnsOnlyThisTripOldestFirst()
    {
        var late = TrackingTestBed.Point(10);
        var early = TrackingTestBed.Point(0);
        await _bed.UploadAsync(TrackingTestBed.Trip, late, early);
        await _bed.UploadAsync(TrackingTestBed.OtherTrip, TrackingTestBed.Point(5));

        var route = await _bed.Service.GetRouteAsync(TrackingTestBed.Trip, CancellationToken.None);

        Assert.True(route.IsSuccess);
        Assert.Equal(new[] { early.ClientPointId, late.ClientPointId }, route.Value.Select(p => p.ClientPointId));
    }

    [Fact]
    public async Task GetRoute_WorksForGuests()
    {
        await _bed.UploadAsync(TrackingTestBed.Point());
        _bed.User.SignOut();

        var route = await _bed.Service.GetRouteAsync(TrackingTestBed.Trip, CancellationToken.None);

        Assert.True(route.IsSuccess);
        Assert.Single(route.Value);
    }

    [Fact]
    public async Task Finish_WhenSignedIn_Succeeds()
    {
        var result = await _bed.Service.FinishRecordingAsync(TrackingTestBed.Trip, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Finish_WhenNotSignedIn_IsForbidden()
    {
        _bed.User.SignOut();

        var result = await _bed.Service.FinishRecordingAsync(TrackingTestBed.Trip, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.FORBIDDEN, "tracking.user.not.authenticated");
    }

    [Fact]
    public async Task DeleteAllForTrip_RemovesOnlyThatTripsRoute()
    {
        await _bed.UploadAsync(TrackingTestBed.Trip, TrackingTestBed.Point(0), TrackingTestBed.Point(1));
        await _bed.UploadAsync(TrackingTestBed.OtherTrip, TrackingTestBed.Point(0));

        var result = await _bed.Service.DeleteAllForTripAsync(TrackingTestBed.Trip, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(_bed.Repository.All, p => Assert.Equal(TrackingTestBed.OtherTrip, p.TripId));
    }
}
