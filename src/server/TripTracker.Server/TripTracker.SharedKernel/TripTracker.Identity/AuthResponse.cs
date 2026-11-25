namespace TripTracker.SharedKernel.TripTracker.Identity;

/// <summary>Tokens issued after a successful sign-in, sign-up or refresh.</summary>
/// <param name="AccessToken">JWT sent in the <c>Authorization: Bearer</c> header.</param>
/// <param name="AccessTokenExpiresAt">When the access token stops being valid.</param>
/// <param name="RefreshToken">Opaque single-use token for getting the next access token.</param>
/// <param name="RefreshTokenExpiresAt">When the refresh token stops being valid.</param>
/// <param name="User">The signed-in account.</param>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserDto User);
