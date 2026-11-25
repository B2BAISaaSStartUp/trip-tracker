using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Contracts;

/// <summary>
/// Removes every photo of a trip. Meant to be called when the trip is deleted (see <c>ITripDeletionHook</c> in the
/// Trips module); it does no access check, so it must not be exposed to clients.
/// </summary>
public interface IPhotoTripCleanup
{
    /// <summary>
    /// Deletes the files first and then the records. Fails, and keeps the records, when the file storage cannot be
    /// reached, so the call can be repeated.
    /// </summary>
    Task<UnitResult<Error>> DeleteAllForTripAsync(Guid tripId, CancellationToken ct);
}
