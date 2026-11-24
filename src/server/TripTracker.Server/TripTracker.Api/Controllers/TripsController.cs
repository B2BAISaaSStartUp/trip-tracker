using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Framework.EndpointResults;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Trips.Contracts;

namespace TripTracker.Api.Controllers;

/// <summary>HTTP endpoints for trips. Maps requests to <see cref="ITripService"/> and returns its result.</summary>
public sealed class TripsController : ApplicationController
{
    private readonly ITripService _trips;

    public TripsController(ITripService trips)
    {
        _trips = trips;
    }

    /// <summary>Creates a private trip owned by the signed-in user.</summary>
    [HttpPost]
    [Authorize]
    public async Task<EndpointResult<TripDto>> Create([FromBody] CreateTripRequest request, CancellationToken ct)
    {
        var result = await _trips.CreateAsync(request, ct);

        return result.IsSuccess
            ? EndpointResult<TripDto>.Created($"/api/trips/{result.Value.Id}", result.Value)
            : result;
    }

    /// <summary>Lists the signed-in user's trips with search, sort and paging.</summary>
    [HttpGet]
    [Authorize]
    public async Task<EndpointResult<PagedList<TripDto>>> List([FromQuery] ListTripsQuery query, CancellationToken ct) =>
        await _trips.ListAsync(query, ct);

    /// <summary>Gets one trip.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<EndpointResult<TripDto>> Get(Guid id, CancellationToken ct) =>
        await _trips.GetAsync(id, ct);

    /// <summary>Updates a trip's title, dates and description.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<EndpointResult<TripDto>> Update(Guid id, [FromBody] UpdateTripRequest request, CancellationToken ct) =>
        await _trips.UpdateAsync(id, request, ct);

    /// <summary>Deletes a trip.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<EndpointResult> Delete(Guid id, CancellationToken ct) =>
        await _trips.DeleteAsync(id, ct);

    /// <summary>Changes who may see a trip.</summary>
    [HttpPut("{id:guid}/access-level")]
    [Authorize]
    public async Task<EndpointResult<TripDto>> ChangeAccessLevel(
        Guid id,
        [FromBody] ChangeAccessLevelRequest request,
        CancellationToken ct) =>
        await _trips.ChangeAccessLevelAsync(id, request.AccessLevel, ct);

    /// <summary>Read-only guest view of a ByLink or Public trip. Needs no sign-in.</summary>
    [HttpGet("shared/{shareToken}")]
    [AllowAnonymous]
    public async Task<EndpointResult<SharedTripDto>> GetShared(string shareToken, CancellationToken ct) =>
        await _trips.GetByShareTokenAsync(shareToken, ct);
}
