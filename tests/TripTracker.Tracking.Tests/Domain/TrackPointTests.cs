using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Tracking.Domain;

namespace TripTracker.Tracking.Tests.Domain;

public class TrackPointTests
{
    private static readonly Guid Trip = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private static Result<TrackPoint, Error> Create(
        double latitude = 50.45,
        double longitude = 30.52,
        double? altitude = 180,
        double? accuracy = 5,
        Guid? clientPointId = null,
        DateTimeOffset? recordedAt = null) =>
        TrackPoint.Create(
            Trip,
            User,
            clientPointId ?? Guid.NewGuid(),
            latitude,
            longitude,
            altitude,
            accuracy,
            recordedAt ?? Now.AddMinutes(-1),
            Now);

    [Fact]
    public void Create_WithValidData_KeepsAllValues()
    {
        var clientPointId = Guid.NewGuid();

        var result = Create(clientPointId: clientPointId);

        Assert.True(result.IsSuccess);
        var point = result.Value;
        Assert.NotEqual(Guid.Empty, point.Id);
        Assert.Equal(Trip, point.TripId);
        Assert.Equal(User, point.RecordedBy);
        Assert.Equal(clientPointId, point.ClientPointId);
        Assert.Equal(50.45, point.Latitude);
        Assert.Equal(30.52, point.Longitude);
        Assert.Equal(Now.AddMinutes(-1), point.RecordedAt);
        Assert.Equal(Now, point.ReceivedAt);
    }

    [Fact]
    public void Create_WithoutAltitudeAndAccuracy_Succeeds()
    {
        var result = Create(altitude: null, accuracy: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Altitude);
        Assert.Null(result.Value.Accuracy);
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(90)]
    public void Create_WithLatitudeOnTheBoundary_Succeeds(double latitude)
    {
        Assert.True(Create(latitude: latitude).IsSuccess);
    }

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_WithInvalidLatitude_IsRejected(double latitude)
    {
        var result = Create(latitude: latitude);

        Assert.True(result.IsFailure);
        Assert.Equal("tracking.point.latitude.invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    public void Create_WithInvalidLongitude_IsRejected(double longitude)
    {
        var result = Create(longitude: longitude);

        Assert.True(result.IsFailure);
        Assert.Equal("tracking.point.longitude.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_WithoutClientPointId_IsRejected()
    {
        var result = TrackPoint.Create(Trip, User, Guid.Empty, 50, 30, null, null, Now, Now);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.VALIDATION, result.Error.Type);
        Assert.Equal("value.is.required", result.Error.Code);
    }

    [Fact]
    public void Create_WithNegativeAccuracy_IsRejected()
    {
        var result = Create(accuracy: -1);

        Assert.True(result.IsFailure);
        Assert.Equal("value.is.invalid", result.Error.Code);
    }

    [Fact]
    public void Create_RecordedFarInTheFuture_IsRejected()
    {
        var result = Create(recordedAt: Now + TrackPoint.MaxClockSkew + TimeSpan.FromSeconds(1));

        Assert.True(result.IsFailure);
        Assert.Equal("tracking.point.time.in.future", result.Error.Code);
    }

    [Fact]
    public void Create_WithDeviceClockSlightlyAhead_Succeeds()
    {
        Assert.True(Create(recordedAt: Now + TrackPoint.MaxClockSkew).IsSuccess);
    }
}
