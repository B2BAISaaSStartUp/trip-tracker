using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Web.Models;

namespace TripTracker.Web.Services;

/// <summary>
/// The REST API calls the web client makes. Failures, including 4xx and 5xx responses and an unreachable
/// server, come back as a failed result. Only a cancellation requested by the caller throws.
/// </summary>
public interface IApiClient
{
    /// <summary>Creates an account and signs it in (<c>POST api/auth/register</c>).</summary>
    Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Signs in with e-mail and password (<c>POST api/auth/login</c>).</summary>
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Lists the signed-in user's trips (<c>GET api/trips</c>).</summary>
    Task<ApiResult<PagedList<TripDto>>> GetTripsAsync(ListTripsQuery query, CancellationToken ct = default);
}
