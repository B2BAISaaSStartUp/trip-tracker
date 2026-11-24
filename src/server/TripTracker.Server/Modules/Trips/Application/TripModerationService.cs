using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Domain;
using TripTracker.Trips.Persistence;

namespace TripTracker.Trips.Application;

internal sealed class TripModerationService : ITripModeration
{
    private readonly ITripRepository _repository;
    private readonly TimeProvider _clock;

    public TripModerationService(ITripRepository repository, TimeProvider clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> HideAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await _repository.GetByIdAsync(tripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound();
        }

        var hidden = trip.Hide(_clock.GetUtcNow());
        if (hidden.IsFailure)
        {
            return hidden;
        }

        await _repository.SaveAsync(trip, ct);
        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> RestoreAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await _repository.GetByIdAsync(tripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound();
        }

        trip.Restore(_clock.GetUtcNow());
        await _repository.SaveAsync(trip, ct);
        return UnitResult.Success<Error>();
    }
}
