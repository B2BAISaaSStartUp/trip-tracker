using CSharpFunctionalExtensions;
using TripTracker.Markers.Contracts;
using TripTracker.Markers.Domain;
using TripTracker.Markers.Persistence;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Markers.Application;

internal sealed class MarkerService : IMarkerService, IMarkerTripCleanup
{
    private readonly IMarkerRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IEnumerable<IMarkerDeletionHook> _deletionHooks;
    private readonly TimeProvider _clock;

    public MarkerService(
        IMarkerRepository repository,
        ICurrentUser currentUser,
        IEnumerable<IMarkerDeletionHook> deletionHooks,
        TimeProvider clock)
    {
        _repository = repository;
        _currentUser = currentUser;
        _deletionHooks = deletionHooks;
        _clock = clock;
    }

    public async Task<Result<MarkerDto, Error>> CreateAsync(Guid tripId, CreateMarkerRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return MarkerErrors.NotAuthenticated();
        }

        var marker = Marker.Create(
            tripId,
            _currentUser.UserId,
            request.Title,
            request.Description,
            request.Category,
            request.Latitude,
            request.Longitude,
            _clock.GetUtcNow());
        if (marker.IsFailure)
        {
            return marker.Error;
        }

        await _repository.AddAsync(marker.Value, ct);
        return marker.Value.ToDto();
    }

    public async Task<Result<MarkerDto, Error>> UpdateAsync(
        Guid tripId,
        Guid markerId,
        UpdateMarkerRequest request,
        CancellationToken ct)
    {
        var marker = await LoadForChangeAsync(tripId, markerId, ct);
        if (marker.IsFailure)
        {
            return marker.Error;
        }

        var updated = marker.Value.Update(
            request.Title,
            request.Description,
            request.Category,
            request.Latitude,
            request.Longitude,
            _clock.GetUtcNow());
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await _repository.SaveAsync(marker.Value, ct);
        return marker.Value.ToDto();
    }

    public async Task<UnitResult<Error>> DeleteAsync(Guid tripId, Guid markerId, CancellationToken ct)
    {
        var marker = await LoadForChangeAsync(tripId, markerId, ct);
        if (marker.IsFailure)
        {
            return marker.Error;
        }

        foreach (var hook in _deletionHooks)
        {
            var hooked = await hook.OnMarkerDeletingAsync(tripId, markerId, ct);
            if (hooked.IsFailure)
            {
                return hooked;
            }
        }

        await _repository.RemoveAsync(marker.Value, ct);
        return UnitResult.Success<Error>();
    }

    public async Task<Result<IReadOnlyList<MarkerDto>, Error>> ListAsync(Guid tripId, CancellationToken ct)
    {
        var markers = await _repository.ListByTripAsync(tripId, ct);

        IReadOnlyList<MarkerDto> dtos = markers.Select(m => m.ToDto()).ToList();
        return Result.Success<IReadOnlyList<MarkerDto>, Error>(dtos);
    }

    public async Task<UnitResult<Error>> DeleteAllForTripAsync(Guid tripId, CancellationToken ct)
    {
        await _repository.RemoveAllForTripAsync(tripId, ct);
        return UnitResult.Success<Error>();
    }

    private async Task<Result<Marker, Error>> LoadForChangeAsync(Guid tripId, Guid markerId, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return MarkerErrors.NotAuthenticated();
        }

        // A marker of another trip is reported as missing: the caller was only checked for access to this trip.
        var marker = await _repository.GetByIdAsync(markerId, ct);
        if (marker is null || marker.TripId != tripId)
        {
            return MarkerErrors.NotFound();
        }

        return marker;
    }
}
