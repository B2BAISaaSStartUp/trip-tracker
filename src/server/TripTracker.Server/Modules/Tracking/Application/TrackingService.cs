using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Tracking;
using TripTracker.Tracking.Contracts;
using TripTracker.Tracking.Domain;
using TripTracker.Tracking.Persistence;

namespace TripTracker.Tracking.Application;

internal sealed class TrackingService : ITrackingService, ITrackingTripCleanup
{
    private readonly ITrackPointRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public TrackingService(ITrackPointRepository repository, ICurrentUser currentUser, TimeProvider clock)
    {
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<TrackUploadResultDto, Error>> UploadPointsAsync(
        Guid tripId,
        UploadTrackPointsRequest request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return TrackingErrors.NotAuthenticated();
        }

        var incoming = request?.Points;
        if (incoming is null || incoming.Count == 0)
        {
            return TrackingErrors.EmptyBatch();
        }

        if (incoming.Count > UploadTrackPointsRequest.MaxPoints)
        {
            return TrackingErrors.BatchTooLarge(UploadTrackPointsRequest.MaxPoints);
        }

        var now = _clock.GetUtcNow();
        var points = new List<TrackPoint>(incoming.Count);
        for (var i = 0; i < incoming.Count; i++)
        {
            var p = incoming[i];
            if (p is null)
            {
                return Errors.Validation.ValueIsRequired($"Points[{i}]");
            }

            var point = TrackPoint.Create(
                tripId,
                _currentUser.UserId,
                p.ClientPointId,
                p.Latitude,
                p.Longitude,
                p.Altitude,
                p.Accuracy,
                p.RecordedAt,
                now);
            if (point.IsFailure)
            {
                return AtIndex(point.Error, i);
            }

            points.Add(point.Value);
        }

        // The app may put the same point into a batch twice (for example after a crash while buffering).
        // The first copy wins.
        var unique = points.DistinctBy(p => p.ClientPointId).ToList();

        var stored = await _repository.AddNewAsync(tripId, unique, ct);
        return new TrackUploadResultDto(incoming.Count, stored, incoming.Count - stored);
    }

    public async Task<Result<IReadOnlyList<TrackPointDto>, Error>> GetRouteAsync(Guid tripId, CancellationToken ct)
    {
        var points = await _repository.ListByTripAsync(tripId, ct);

        IReadOnlyList<TrackPointDto> dtos = points.Select(p => p.ToDto()).ToList();
        return Result.Success<IReadOnlyList<TrackPointDto>, Error>(dtos);
    }

    public Task<UnitResult<Error>> FinishRecordingAsync(Guid tripId, CancellationToken ct)
    {
        // STUB: nothing to finalize yet. Route summary and simplification will be added here.
        UnitResult<Error> result = _currentUser.IsAuthenticated
            ? UnitResult.Success<Error>()
            : TrackingErrors.NotAuthenticated();

        return Task.FromResult(result);
    }

    public async Task<UnitResult<Error>> DeleteAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        await _repository.RemoveAllForTripAsync(tripId, ct);
        return UnitResult.Success<Error>();
    }

    /// <summary>Points a validation error at the batch item that caused it, e.g. <c>Points[3].Latitude</c>.</summary>
    private static Error AtIndex(Error error, int index)
    {
        var field = error.InvalidField is null ? $"Points[{index}]" : $"Points[{index}].{error.InvalidField}";
        return Error.Validation(error.Code, $"Points[{index}]: {error.Message}", field);
    }
}
