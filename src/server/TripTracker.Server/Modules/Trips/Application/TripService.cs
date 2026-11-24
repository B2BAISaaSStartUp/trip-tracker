using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Core.Validation;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Domain;
using TripTracker.Trips.Persistence;

namespace TripTracker.Trips.Application;

internal sealed class TripService : ITripService
{
    private readonly ITripRepository _repository;
    private readonly ITripAccess _access;
    private readonly ICurrentUser _currentUser;
    private readonly IEnumerable<ITripDeletionHook> _deletionHooks;
    private readonly TimeProvider _clock;

    public TripService(
        ITripRepository repository,
        ITripAccess access,
        ICurrentUser currentUser,
        IEnumerable<ITripDeletionHook> deletionHooks,
        TimeProvider clock)
    {
        _repository = repository;
        _access = access;
        _currentUser = currentUser;
        _deletionHooks = deletionHooks;
        _clock = clock;
    }

    public async Task<Result<TripDto, Error>> CreateAsync(CreateTripRequest request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return TripErrors.NotAuthenticated();
        }

        var userId = _currentUser.UserId;
        var trip = Trip.Create(
            userId,
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            _clock.GetUtcNow());
        if (trip.IsFailure)
        {
            return trip.Error;
        }

        await _repository.AddAsync(trip.Value, ct);
        return trip.Value.ToDto(includeShareToken: true);
    }

    public async Task<Result<PagedList<TripDto>, Error>> ListAsync(ListTripsQuery query, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return TripErrors.NotAuthenticated();
        }

        var validation = new ListTripsQueryValidator().Validate(query);
        if (!validation.IsValid)
        {
            return validation.ToList().First();
        }

        var page = await _repository.ListByOwnerAsync(_currentUser.UserId, query, ct);

        // Every trip on this page belongs to the current user, so the share token is shown.
        return new PagedList<TripDto>
        {
            Items = page.Items.Select(t => t.ToDto(includeShareToken: true)).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize,
        };
    }

    public async Task<Result<TripDto, Error>> GetAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await LoadAsync(tripId, TripAccessKind.Read, ct);
        if (trip.IsFailure)
        {
            return trip.Error;
        }

        return trip.Value.ToDto(includeShareToken: IsOwner(trip.Value));
    }

    public async Task<Result<TripDto, Error>> UpdateAsync(Guid tripId, UpdateTripRequest request, CancellationToken ct)
    {
        var trip = await LoadAsync(tripId, TripAccessKind.Write, ct);
        if (trip.IsFailure)
        {
            return trip.Error;
        }

        var updated = trip.Value.Update(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            _clock.GetUtcNow());
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await _repository.SaveAsync(trip.Value, ct);
        return trip.Value.ToDto(includeShareToken: true);
    }

    public async Task<UnitResult<Error>> DeleteAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await LoadAsync(tripId, TripAccessKind.Write, ct);
        if (trip.IsFailure)
        {
            return trip.Error;
        }

        foreach (var hook in _deletionHooks)
        {
            var hooked = await hook.OnTripDeletingAsync(tripId, ct);
            if (hooked.IsFailure)
            {
                return hooked;
            }
        }

        await _repository.RemoveAsync(trip.Value, ct);
        return UnitResult.Success<Error>();
    }

    public async Task<Result<TripDto, Error>> ChangeAccessLevelAsync(
        Guid tripId,
        TripAccessLevel level,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(level))
        {
            return Errors.Validation.ValueIsInvalid("AccessLevel", "unknown access level");
        }

        var trip = await LoadAsync(tripId, TripAccessKind.Write, ct);
        if (trip.IsFailure)
        {
            return trip.Error;
        }

        trip.Value.ChangeAccessLevel(level, _clock.GetUtcNow());
        await _repository.SaveAsync(trip.Value, ct);
        return trip.Value.ToDto(includeShareToken: true);
    }

    public async Task<Result<SharedTripDto, Error>> GetByShareTokenAsync(string shareToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return TripErrors.NotFound();
        }

        var found = await _repository.GetByShareTokenAsync(shareToken, ct);
        if (found is null)
        {
            return TripErrors.NotFound();
        }

        var trip = await LoadAsync(found.Id, TripAccessKind.Read, ct, shareToken);
        if (trip.IsFailure)
        {
            return trip.Error;
        }

        return trip.Value.ToSharedDto();
    }

    private async Task<Result<Trip, Error>> LoadAsync(
        Guid tripId,
        TripAccessKind kind,
        CancellationToken ct,
        string? shareToken = null)
    {
        var userId = _currentUser.IsAuthenticated ? _currentUser.UserId : (Guid?)null;

        var access = await _access.CheckAsync(new TripAccessRequest(tripId, userId, shareToken, kind), ct);
        if (access.IsFailure)
        {
            return access.Error;
        }

        var trip = await _repository.GetByIdAsync(tripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound();
        }

        return trip;
    }

    private bool IsOwner(Trip trip) => _currentUser.IsAuthenticated && _currentUser.UserId == trip.OwnerId;
}
