using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TripTracker.Identity.Domain;

namespace TripTracker.Identity.Persistence;

/// <summary>EF Core context for the "identity" schema: accounts, external logins and refresh tokens.</summary>
internal sealed class IdentityStoreDbContext : IdentityUserContext<AppUser, Guid>
{
    public const string SchemaName = "identity";

    public IdentityStoreDbContext(DbContextOptions<IdentityStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(SchemaName);

        builder.Entity<AppUser>(user =>
        {
            user.ToTable("users");
            user.Property(u => u.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength).IsRequired();
            user.Property(u => u.Role).HasMaxLength(AppUser.RoleMaxLength).IsRequired();
            user.Property(u => u.IsBlocked).IsRequired();
            user.Property(u => u.CreatedAt).IsRequired();
        });

        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.ApplyConfiguration(new RefreshTokenConfiguration());
    }
}
