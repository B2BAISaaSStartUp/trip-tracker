using CSharpFunctionalExtensions;
using TripTracker.Markers.Application;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Tests.Fakes;

/// <summary>The real service over an in-memory repository, a fake clock and a fake user signed in as <see cref="Owner"/>.</summary>
internal sealed class MarkersTestBed
{
    public static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Trip = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid OtherTrip = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public MarkersTestBed()
    {
        Repository = new InMemoryMarkerRepository();
        User = new FakeCurrentUser();
        User.SignInAs(Owner);
        Clock = new FakeClock();
        Hook = new RecordingDeletionHook();
        Service = new MarkerService(Repository, User, [Hook], Clock);
    }

    public InMemoryMarkerRepository Repository { get; }

    public FakeCurrentUser User { get; }

    public FakeClock Clock { get; }

    public RecordingDeletionHook Hook { get; }

    public MarkerService Service { get; }

    public static CreateMarkerRequest NewMarker(string title = "Old Town", MarkerCategory category = MarkerCategory.Sight) =>
        new(title, "Meet here", category, 50.45, 30.52);

    public static UpdateMarkerRequest NewUpdate(string title = "Cafe", MarkerCategory category = MarkerCategory.Food) =>
        new(title, null, category, 49.84, 24.03);

    public Task<Result<MarkerDto, Error>> CreateAsync(Guid? tripId = null, string title = "Old Town") =>
        Service.CreateAsync(tripId ?? Trip, NewMarker(title), CancellationToken.None);
}
