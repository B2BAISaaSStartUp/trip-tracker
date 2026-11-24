using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Trips.Contracts;

/// <summary>Moderation of public trips (FR-21). The caller is responsible for checking the Admin role.</summary>
public interface ITripModeration
{
    /// <summary>Hides a public trip from everyone except its owner. Fails with Conflict if the trip is not public.</summary>
    Task<UnitResult<Error>> HideAsync(Guid tripId, CancellationToken ct);

    /// <summary>Makes a hidden trip visible again.</summary>
    Task<UnitResult<Error>> RestoreAsync(Guid tripId, CancellationToken ct);
}
