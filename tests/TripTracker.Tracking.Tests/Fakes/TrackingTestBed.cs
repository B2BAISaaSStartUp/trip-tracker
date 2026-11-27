using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Tracking;
using TripTracker.Tracking.Application;

namespace TripTracker.Tracking.Tests.Fakes;

/// <summary>The real service over an in-memory repository and a fake current user, signed in as <see cref="Owner"/>.</summary>
internal sealed class TrackingTestBed
{
    public static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Trip = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid OtherTrip = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly DateTimeOffset Start = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    public TrackingTestBed()
    {
        Repository = new InMemoryTrackPointRepository();
        User = new FakeCurrentUser();
        User.SignInAs(Owner);
        Service = new TrackingService(Repository, User, TimeProvider.System);
    }

    public InMemoryTrackPointRepository Repository { get; }

    public FakeCurrentUser User { get; }

    public TrackingService Service { get; }

    /// <summary>A valid point recorded <paramref name="minute"/> minutes after <see cref="Start"/>.</summary>
    public static TrackPointRequest Point(int minute = 0, Guid? clientPointId = null, double latitude = 50.45) =>
        new(clientPointId ?? Guid.NewGuid(), latitude, 30.52, 180, 5, Start.AddMinutes(minute));

    public Task<Result<TrackUploadResultDto, Error>> UploadAsync(Guid tripId, params TrackPointRequest[] points) =>
        Service.UploadPointsAsync(tripId, new UploadTrackPointsRequest(points), CancellationToken.None);

    public Task<Result<TrackUploadResultDto, Error>> UploadAsync(params TrackPointRequest[] points) =>
        UploadAsync(Trip, points);
}
