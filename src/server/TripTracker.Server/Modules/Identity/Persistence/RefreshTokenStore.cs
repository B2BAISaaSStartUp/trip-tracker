using Microsoft.EntityFrameworkCore;
using TripTracker.Identity.Domain;

namespace TripTracker.Identity.Persistence;

/// <summary>EF Core implementation of <see cref="IRefreshTokenStore"/>.</summary>
internal sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly IdentityStoreDbContext _db;

    public RefreshTokenStore(IdentityStoreDbContext db)
    {
        _db = db;
    }

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.Revoke(now);
        }

        await _db.SaveChangesAsync(ct);
    }
}
