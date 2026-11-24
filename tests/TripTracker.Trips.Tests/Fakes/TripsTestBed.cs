using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Application;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Domain;

namespace TripTracker.Trips.Tests.Fakes;

/// <summary>Wires the Trips services to an in-memory repository and a fake current user.</summary>
internal sealed class TripsTestBed
{
    public static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Stranger = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateOnly Start = new(2026, 7, 1);
    public static readonly DateOnly End = new(2026, 7, 10);

    public TripsTestBed()
    {
        Repository = new InMemoryTripRepository();
        User = new FakeCurrentUser();
        Hooks = [];
        Access = new TripAccessService(Repository);
        Service = new TripService(Repository, Access, User, Hooks, TimeProvider.System);
        Moderation = new TripModerationService(Repository, TimeProvider.System);
    }

    public InMemoryTripRepository Repository { get; }

    public FakeCurrentUser User { get; }

    public List<ITripDeletionHook> Hooks { get; }

    public TripAccessService Access { get; }

    public TripService Service { get; }

    public TripModerationService Moderation { get; }

    /// <summary>Puts a trip straight into the repository, bypassing the current user.</summary>
    public Trip SeedTrip(
        Guid ownerId,
        string title = "Alps",
        TripAccessLevel level = TripAccessLevel.Private,
        DateOnly? start = null)
    {
        var startDate = start ?? Start;
        var trip = Trip.Create(ownerId, title, null, startDate, startDate.AddDays(9), DateTimeOffset.UtcNow).Value;
        trip.ChangeAccessLevel(level, DateTimeOffset.UtcNow);
        Repository.Seed(trip);
        return trip;
    }
}
