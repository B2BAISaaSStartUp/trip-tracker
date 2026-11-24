using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;

namespace TripTracker.Trips.Contracts;

/// <summary>Trip operations for the signed-in user (FR-06, FR-07, FR-08, FR-19, FR-20).</summary>
public interface ITripService
{
    /// <summary>Creates a private trip owned by the current user.</summary>
    Task<Result<TripDto, Error>> CreateAsync(CreateTripRequest request, CancellationToken ct);

    /// <summary>Lists the current user's trips.</summary>
    Task<Result<PagedList<TripDto>, Error>> ListAsync(ListTripsQuery query, CancellationToken ct);

    /// <summary>Gets a trip the current user may read.</summary>
    Task<Result<TripDto, Error>> GetAsync(Guid tripId, CancellationToken ct);

    /// <summary>Changes title, dates and description. Owner only.</summary>
    Task<Result<TripDto, Error>> UpdateAsync(Guid tripId, UpdateTripRequest request, CancellationToken ct);

    /// <summary>Deletes a trip. Owner only.</summary>
    Task<UnitResult<Error>> DeleteAsync(Guid tripId, CancellationToken ct);

    /// <summary>Changes who may see the trip. Owner only.</summary>
    Task<Result<TripDto, Error>> ChangeAccessLevelAsync(Guid tripId, TripAccessLevel level, CancellationToken ct);

    /// <summary>Returns the read-only guest view of a ByLink or Public trip. Works without signing in.</summary>
    Task<Result<SharedTripDto, Error>> GetByShareTokenAsync(string shareToken, CancellationToken ct);
}
