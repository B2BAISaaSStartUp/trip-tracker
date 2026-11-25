using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Contracts;

/// <summary>Sign-up, sign-in and token handling for every client (FR-01, FR-02, FR-04).</summary>
public interface IAuthService
{
    /// <summary>Creates a User account and signs it in. Fails with AlreadyExists when the e-mail is taken.</summary>
    Task<Result<AuthResponse, Error>> RegisterAsync(RegisterRequest request, CancellationToken ct);

    /// <summary>Signs in with e-mail and password. Repeated failures lock the account for a while.</summary>
    Task<Result<AuthResponse, Error>> LoginAsync(LoginRequest request, CancellationToken ct);

    /// <summary>Signs in with a Google ID token; creates the account on first use.</summary>
    Task<Result<AuthResponse, Error>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct);

    /// <summary>Exchanges a refresh token for a new access token and a new refresh token. The old refresh token stops working.</summary>
    Task<Result<AuthResponse, Error>> RefreshAsync(RefreshRequest request, CancellationToken ct);

    /// <summary>Revokes a refresh token. Always succeeds, so a client can call it with an expired access token.</summary>
    Task<UnitResult<Error>> LogoutAsync(RefreshRequest request, CancellationToken ct);

    /// <summary>Returns the signed-in account.</summary>
    Task<Result<UserDto, Error>> GetCurrentUserAsync(CancellationToken ct);
}
