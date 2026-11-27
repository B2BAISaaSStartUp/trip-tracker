using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Contracts;

/// <summary>
/// EXTENSION POINT: deleting a marker must also update data of other modules that points at it (photos attached to
/// the marker). Those modules register an implementation in the host; Markers does not know them.
/// </summary>
public interface IMarkerDeletionHook
{
    /// <summary>Called before the marker is removed. A failure cancels the deletion.</summary>
    Task<UnitResult<Error>> OnMarkerDeletingAsync(Guid tripId, Guid markerId, CancellationToken ct);
}
