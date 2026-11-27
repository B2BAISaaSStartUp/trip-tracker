using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Framework.EndpointResults;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Tracking;
using TripTracker.Tracking.Contracts;
using TripTracker.Trips.Contracts;

namespace TripTracker.Api.Controllers;

/// <summary>
/// HTTP endpoints for the recorded route of a trip. This is the trust boundary: every action first asks
/// <see cref="ITripAccess"/> (Read or Write) and only then calls <see cref="ITrackingService"/>.
/// Guests read through a share link by passing <c>shareToken</c>.
/// </summary>
public sealed class TripTrackController : ApplicationController
{
    private readonly ITrackingService _tracking;
    private readonly ITripAccess _access;
    private readonly ICurrentUser _currentUser;

    public TripTrackController(ITrackingService tracking, ITripAccess access, ICurrentUser currentUser)
    {
        _tracking = tracking;
        _access = access;
        _currentUser = currentUser;
    }

    /// <summary>Uploads a batch of route points. Resending the same points is safe: they are stored once.</summary>
    [HttpPost("~/api/trips/{tripId:guid}/track/points")]
    [Authorize]
    public async Task<EndpointResult<TrackUploadResultDto>> UploadPoints(
        Guid tripId,
        [FromBody] UploadTrackPointsRequest request,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult<TrackUploadResultDto>.FromErrors(access.Error.ToErrorList());
        }

        return await _tracking.UploadPointsAsync(tripId, request, ct);
    }

    /// <summary>Returns the route of a trip, oldest point first.</summary>
    [HttpGet("~/api/trips/{tripId:guid}/track")]
    [AllowAnonymous]
    public async Task<EndpointResult<IReadOnlyList<TrackPointDto>>> GetRoute(
        Guid tripId,
        [FromQuery] string? shareToken,
        CancellationToken ct)
    {
        var access = await CheckAsync(tripId, shareToken, TripAccessKind.Read, ct);
        if (access.IsFailure)
        {
            return EndpointResult<IReadOnlyList<TrackPointDto>>.FromErrors(access.Error.ToErrorList());
        }

        return await _tracking.GetRouteAsync(tripId, ct);
    }

    /// <summary>Marks the end of route recording (stub for now).</summary>
    [HttpPost("~/api/trips/{tripId:guid}/track/finish")]
    [Authorize]
    public async Task<EndpointResult> Finish(Guid tripId, CancellationToken ct)
    {
        var access = await CheckAsync(tripId, null, TripAccessKind.Write, ct);
        if (access.IsFailure)
        {
            return EndpointResult.FromErrors(access.Error.ToErrorList());
        }

        return await _tracking.FinishRecordingAsync(tripId, ct);
    }

    private Task<UnitResult<Error>> CheckAsync(Guid tripId, string? shareToken, TripAccessKind kind, CancellationToken ct)
    {
        Guid? userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        return _access.CheckAsync(new TripAccessRequest(tripId, userId, shareToken, kind), ct);
    }
}
