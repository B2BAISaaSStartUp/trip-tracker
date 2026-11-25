using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripTracker.Identity.Contracts;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Framework.EndpointResults;
using TripTracker.SharedKernel.TripTracker.Identity;

namespace TripTracker.Api.Controllers;

/// <summary>HTTP endpoints for sign-up, sign-in and tokens. Maps requests to <see cref="IAuthService"/> and returns its result.</summary>
public sealed class AuthController : ApplicationController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Creates an account and signs it in.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<EndpointResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(request, ct);

        return result.IsSuccess
            ? EndpointResult<AuthResponse>.Created("/api/auth/me", result.Value)
            : result;
    }

    /// <summary>Signs in with e-mail and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<EndpointResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        await _auth.LoginAsync(request, ct);

    /// <summary>Signs in with a Google ID token.</summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<EndpointResult<AuthResponse>> Google([FromBody] GoogleLoginRequest request, CancellationToken ct) =>
        await _auth.LoginWithGoogleAsync(request, ct);

    /// <summary>Exchanges a refresh token for new tokens.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<EndpointResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct) =>
        await _auth.RefreshAsync(request, ct);

    /// <summary>Revokes a refresh token. Needs no access token, because it may already have expired.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<EndpointResult> Logout([FromBody] RefreshRequest request, CancellationToken ct) =>
        await _auth.LogoutAsync(request, ct);

    /// <summary>Returns the signed-in account.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<EndpointResult<UserDto>> Me(CancellationToken ct) =>
        await _auth.GetCurrentUserAsync(ct);
}
