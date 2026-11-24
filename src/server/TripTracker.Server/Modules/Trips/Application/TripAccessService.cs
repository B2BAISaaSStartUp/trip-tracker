using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Domain;
using TripTracker.Trips.Persistence;

namespace TripTracker.Trips.Application;

/// <summary>Decides who may read or write a trip. Every access rule of FR-19 lives here.</summary>
internal sealed class TripAccessService : ITripAccess
{
    private readonly ITripRepository _repository;

    public TripAccessService(ITripRepository repository)
    {
        _repository = repository;
    }

    public async Task<UnitResult<Error>> CheckAsync(TripAccessRequest request, CancellationToken ct)
    {
        var trip = await _repository.GetByIdAsync(request.TripId, ct);

        if (trip is null)
        {
            return TripErrors.NotFound();
        }

        // The owner can always read and write, even when moderation hid the trip.
        if (request.UserId is { } userId && userId == trip.OwnerId)
        {
            return UnitResult.Success<Error>();
        }

        // From here on the caller is not the owner. NotFound (not Forbidden) keeps invisible trips secret.
        if (trip.IsHidden || !IsVisibleToNonOwner(trip, request.ShareToken))
        {
            return TripErrors.NotFound();
        }

        return request.Kind == TripAccessKind.Read
            ? UnitResult.Success<Error>()
            : TripErrors.NotOwner();
    }

    private static bool IsVisibleToNonOwner(Trip trip, string? shareToken) =>
        trip.AccessLevel switch
        {
            TripAccessLevel.Public => true,
            TripAccessLevel.ByLink => TokenMatches(trip.ShareToken, shareToken),
            _ => false,
        };

    private static bool TokenMatches(string expected, string? provided) =>
        !string.IsNullOrEmpty(provided)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
}
