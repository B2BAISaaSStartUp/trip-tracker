using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Trips.Contracts;

/// <summary>The single place that decides who may read or write a trip (FR-19).</summary>
public interface ITripAccess
{
    /// <summary>
    /// Succeeds when the caller may do what was asked. Fails with NotFound when the trip is missing or
    /// not visible to the caller, and with Forbidden when it is visible but not writable.
    /// </summary>
    Task<UnitResult<Error>> CheckAsync(TripAccessRequest request, CancellationToken ct);
}
