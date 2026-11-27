using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Tracking;

namespace TripTracker.Tracking.Contracts;

/// <summary>
/// The recorded route of a trip. These methods do not check access to the trip: the caller (the REST layer) must
/// first ask <c>ITripAccess</c> - Read for reading, Write for everything else. Changes still require a signed-in user.
/// </summary>
public interface ITrackingService
{
    /// <summary>
    /// Stores a batch of points. Idempotent (NFR-08): a point whose <c>ClientPointId</c> is already stored for the
    /// trip is skipped, so resending a batch after a lost response stores nothing twice. The batch is all or
    /// nothing: if one point is invalid, no point is stored and the error names the point.
    /// </summary>
    Task<Result<TrackUploadResultDto, Error>> UploadPointsAsync(
        Guid tripId,
        UploadTrackPointsRequest request,
        CancellationToken ct);

    /// <summary>Returns the route of a trip, oldest point first.</summary>
    Task<Result<IReadOnlyList<TrackPointDto>, Error>> GetRouteAsync(Guid tripId, CancellationToken ct);

    /// <summary>
    /// STUB: marks the end of route recording. Today it only checks that the caller is signed in; route summary
    /// (distance, duration) and simplification will be added here later without changing the signature.
    /// </summary>
    Task<UnitResult<Error>> FinishRecordingAsync(Guid tripId, CancellationToken ct);
}
