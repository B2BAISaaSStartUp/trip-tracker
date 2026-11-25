using TripTracker.Identity.Domain;

namespace TripTracker.Identity.Security;

/// <summary>A signed JWT and the moment it expires.</summary>
internal sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>A fresh refresh token: the secret for the client and the hash to store.</summary>
internal sealed record NewRefreshToken(string Raw, string Hash, DateTimeOffset ExpiresAt);

/// <summary>Creates and hashes tokens.</summary>
internal interface ITokenService
{
    AccessToken CreateAccessToken(AppUser user);

    NewRefreshToken CreateRefreshToken();

    /// <summary>Hash under which a refresh token is stored and looked up.</summary>
    string HashRefreshToken(string rawToken);
}
