using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripTracker.Markers.Contracts;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Framework.EndpointResults;
using TripTracker.SharedKernel.TripTracker.Markers;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Trips.Contracts;

namespace TripTracker.Api.Controllers;

/// <summary>
/// HTTP endpoints for the markers of a trip. This is the trust boundary: every action first asks
/// <see cref="ITripAccess"/> (Read or Write) and only then calls <see cref="IMarkerService"/>.
/// Guests read through a share link by passing <c>shareToken</c>.
/// </summary>
public sealed class TripMarkersController : ApplicationController
{
    private readonly IMarkerService _markers;
    private readonly ITripAccess _access;
    private readonly ICurrentUser _currentUser;

    public TripMarkersController(IMarkerService markers, ITripAccess access, ICurrentUser currentUser)
    {
        _markers = markers;
        _access = access;
        _currentUser = currentUser;
    }

    /// <summary>Lists the markers of a trip, oldest first.</summary>
    [HttpGet("~/api/trips/{tripId:guid}/markers")]
    [AllowAnonymous]
    public async Task<EndpointResult<IReadOnlyList<MarkerDto>>> List(
        Guid tripId,
        [FromQuery] string? shareToken,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, shareToken, TripAccessKind.Read, ct);
        if (access.IsFailure)
        {
            return EndpointResult<IReadOnlyList<MarkerDto>>.FromErrors(access.Error.ToErrorList());
        }

        return await _markers.ListAsync(tripId, ct);
    }

    /// <summary>Puts a marker on the map of a trip the signed-in user owns.</summary>
    [HttpPost("~/api/trips/{tripId:guid}/markers")]
    [Authorize]
    public async Task<EndpointResult<MarkerDto>> Create(
        Guid tripId,
        [FromBody] CreateMarkerRequest request,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult<MarkerDto>.FromErrors(access.Error.ToErrorList());
        }

        var result = await _markers.CreateAsync(tripId, request, ct);

        return result.IsSuccess
            ? EndpointResult<MarkerDto>.Created($"/api/trips/{tripId}/markers/{result.Value.Id}", result.Value)
            : result;
    }

    /// <summary>Changes a marker's title, description, category and position.</summary>
    [HttpPut("~/api/trips/{tripId:guid}/markers/{markerId:guid}")]
    [Authorize]
    public async Task<EndpointResult<MarkerDto>> Update(
        Guid tripId,
        Guid markerId,
        [FromBody] UpdateMarkerRequest request,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult<MarkerDto>.FromErrors(access.Error.ToErrorList());
        }

        return await _markers.UpdateAsync(tripId, markerId, request, ct);
    }

    /// <summary>Deletes a marker; its photos stay in the trip gallery.</summary>
    [HttpDelete("~/api/trips/{tripId:guid}/markers/{markerId:guid}")]
    [Authorize]
    public async Task<EndpointResult> Delete(Guid tripId, Guid markerId, CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult.FromErrors(access.Error.ToErrorList());
        }

        return await _markers.DeleteAsync(tripId, markerId, ct);
    }

    private Task<UnitResult<Error>> CheckAsync(Guid tripId, string? shareToken, TripAccessKind kind, CancellationToken ct)
    {
        Guid? userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        return _access.CheckAsync(new TripAccessRequest(tripId, userId, shareToken, kind), ct);
    }
}
