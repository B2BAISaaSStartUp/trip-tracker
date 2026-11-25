using TripTracker.Identity.Domain;

namespace TripTracker.Identity.Persistence;

/// <summary>Storage for refresh tokens.</summary>
internal interface IRefreshTokenStore
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>Adds a token and saves it together with any other pending change to tokens returned by this store.</summary>
    Task AddAsync(RefreshToken token, CancellationToken ct);

    /// <summary>Saves changes made to tokens returned by this store.</summary>
    Task SaveAsync(CancellationToken ct);

    /// <summary>Revokes every token of the user that is still active.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct);
}
