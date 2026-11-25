namespace TripTracker.Identity.Domain;

/// <summary>A refresh token as stored: only its hash, never the token itself.</summary>
internal sealed class RefreshToken
{
    // Required by EF Core.
    private RefreshToken()
    {
        TokenHash = null!;
    }

    private RefreshToken(Guid userId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>The token that replaced this one when it was rotated.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset now, DateTimeOffset expiresAt) =>
        new(userId, tokenHash, now, expiresAt);

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>Marks the token as unusable. Revoking twice keeps the first time.</summary>
    public void Revoke(DateTimeOffset now, Guid? replacedBy = null)
    {
        RevokedAt ??= now;
        ReplacedByTokenId ??= replacedBy;
    }
}
