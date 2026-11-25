using System.Text;

namespace TripTracker.Identity.Security;

/// <summary>Settings for access and refresh tokens, read from <c>Identity:Jwt</c>.</summary>
internal sealed class JwtOptions
{
    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = "TripTracker";

    public string Audience { get; set; } = "TripTracker.Clients";

    /// <summary>Secret used to sign access tokens. Keep it in user secrets or an environment variable, never in a committed file.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes (NFR-11: one hour).</summary>
    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>Refresh token lifetime in days.</summary>
    public int RefreshTokenDays { get; set; } = 30;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);

    /// <exception cref="InvalidOperationException">A setting is missing or out of range.</exception>
    public void Validate()
    {
        if (Encoding.UTF8.GetByteCount(SigningKey) < MinSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"'Identity:Jwt:SigningKey' must be set and at least {MinSigningKeyBytes} bytes long.");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("'Identity:Jwt:Issuer' and 'Identity:Jwt:Audience' must be set.");
        }

        if (AccessTokenMinutes < 1 || RefreshTokenDays < 1)
        {
            throw new InvalidOperationException(
                "'Identity:Jwt:AccessTokenMinutes' and 'Identity:Jwt:RefreshTokenDays' must be at least 1.");
        }
    }
}
