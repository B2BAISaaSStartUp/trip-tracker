using CSharpFunctionalExtensions;
using TripTracker.Markers.Contracts;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Tests.Fakes;

/// <summary>Remembers which markers it was asked about; fails with <see cref="FailWith"/> when it is set.</summary>
internal sealed class RecordingDeletionHook : IMarkerDeletionHook
{
    public List<(Guid TripId, Guid MarkerId)> Calls { get; } = [];

    public Error? FailWith { get; set; }

    public Task<UnitResult<Error>> OnMarkerDeletingAsync(Guid tripId, Guid markerId, CancellationToken ct)
    {
        Calls.Add((tripId, markerId));

        UnitResult<Error> result = FailWith is { } error
            ? UnitResult.Failure(error)
            : UnitResult.Success<Error>();

        return Task.FromResult(result);
    }
}
