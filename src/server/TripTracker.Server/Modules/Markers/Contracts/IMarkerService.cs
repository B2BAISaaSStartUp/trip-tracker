using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Contracts;

/// <summary>
/// Markers on a trip's map. These methods do not check access to the trip: the caller (the REST layer) must first
/// ask <c>ITripAccess</c> - Read for listing, Write for everything else. Changes still require a signed-in user.
/// </summary>
public interface IMarkerService
{
    /// <summary>Puts a marker on the trip's map.</summary>
    Task<Result<MarkerDto, Error>> CreateAsync(Guid tripId, CreateMarkerRequest request, CancellationToken ct);

    /// <summary>Replaces a marker's title, description, category and position.</summary>
    Task<Result<MarkerDto, Error>> UpdateAsync(
        Guid tripId,
        Guid markerId,
        UpdateMarkerRequest request,
        CancellationToken ct);

    /// <summary>Deletes a marker. Every <see cref="IMarkerDeletionHook"/> runs first; a failing hook cancels it.</summary>
    Task<UnitResult<Error>> DeleteAsync(Guid tripId, Guid markerId, CancellationToken ct);

    /// <summary>Lists the markers of a trip, oldest first.</summary>
    Task<Result<IReadOnlyList<MarkerDto>, Error>> ListAsync(Guid tripId, CancellationToken ct);
}
