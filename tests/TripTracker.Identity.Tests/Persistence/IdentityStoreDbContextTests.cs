using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Persistence;

namespace TripTracker.Identity.Tests.Persistence;

/// <summary>Checks the EF Core mapping without a database: model shape and round trip.</summary>
public class IdentityStoreDbContextTests
{
    // Never opened: the model does not connect. Contains no real credentials.
    private const string OfflineConnectionString = "Host=localhost;Database=offline";

    private static IdentityStoreDbContext NewNpgsqlContext() =>
        new(new DbContextOptionsBuilder<IdentityStoreDbContext>().UseNpgsql(OfflineConnectionString).Options);

    [Fact]
    public void Model_UsesTheIdentitySchemaAndSnakeCaseTables()
    {
        using var db = NewNpgsqlContext();

        Assert.Equal("identity", db.Model.FindEntityType(typeof(AppUser))!.GetSchema());
        Assert.Equal("users", db.Model.FindEntityType(typeof(AppUser))!.GetTableName());
        Assert.Equal("refresh_tokens", db.Model.FindEntityType(typeof(RefreshToken))!.GetTableName());
        Assert.Equal("user_logins", db.Model.FindEntityType(typeof(IdentityUserLogin<Guid>))!.GetTableName());
        Assert.All(db.Model.GetEntityTypes(), e => Assert.Equal("identity", e.GetSchema()));
    }

    [Fact]
    public void Model_HasNoRoleTables()
    {
        using var db = NewNpgsqlContext();

        Assert.DoesNotContain(db.Model.GetEntityTypes(), e => e.ClrType == typeof(IdentityRole<Guid>));
    }

    [Fact]
    public void RefreshTokenHash_IsUnique()
    {
        using var db = NewNpgsqlContext();

        var entity = db.Model.FindEntityType(typeof(RefreshToken))!;
        var hashProperty = entity.FindProperty(nameof(RefreshToken.TokenHash))!;

        Assert.Contains(entity.GetIndexes(), i => i.IsUnique && i.Properties.Contains(hashProperty));
    }

    [Fact]
    public async Task RefreshToken_RoundTripsThroughTheStore()
    {
        var options = new DbContextOptionsBuilder<IdentityStoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = DateTimeOffset.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "ABC123", now, now.AddDays(30));

        await using (var writer = new IdentityStoreDbContext(options))
        {
            await new RefreshTokenStore(writer).AddAsync(token, CancellationToken.None);
        }

        await using var reader = new IdentityStoreDbContext(options);
        var loaded = await new RefreshTokenStore(reader).FindByHashAsync("ABC123", CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(token.Id, loaded.Id);
        Assert.Equal(token.UserId, loaded.UserId);
        Assert.False(loaded.IsRevoked);
        Assert.Null(await new RefreshTokenStore(reader).FindByHashAsync("other", CancellationToken.None));
    }

    [Fact]
    public async Task RevokeAllForUser_RevokesOnlyThatUsersActiveTokens()
    {
        var options = new DbContextOptionsBuilder<IdentityStoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = DateTimeOffset.UtcNow;
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();

        await using var db = new IdentityStoreDbContext(options);
        var store = new RefreshTokenStore(db);
        await store.AddAsync(RefreshToken.Create(user, "U1", now, now.AddDays(1)), CancellationToken.None);
        await store.AddAsync(RefreshToken.Create(user, "U2", now, now.AddDays(1)), CancellationToken.None);
        await store.AddAsync(RefreshToken.Create(other, "O1", now, now.AddDays(1)), CancellationToken.None);

        await store.RevokeAllForUserAsync(user, now, CancellationToken.None);

        Assert.True((await store.FindByHashAsync("U1", CancellationToken.None))!.IsRevoked);
        Assert.True((await store.FindByHashAsync("U2", CancellationToken.None))!.IsRevoked);
        Assert.False((await store.FindByHashAsync("O1", CancellationToken.None))!.IsRevoked);
    }
}
