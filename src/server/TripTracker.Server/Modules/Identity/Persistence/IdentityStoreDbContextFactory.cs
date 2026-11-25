using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TripTracker.Identity.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c>. Reads the connection string from the <c>ConnectionStrings__Identity</c>
/// environment variable, so no credentials live in the repository.
/// </summary>
internal sealed class IdentityStoreDbContextFactory : IDesignTimeDbContextFactory<IdentityStoreDbContext>
{
    public IdentityStoreDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set the ConnectionStrings__Identity environment variable before running dotnet ef.");
        }

        var options = new DbContextOptionsBuilder<IdentityStoreDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                IdentityModule.MigrationsHistoryTable,
                IdentityStoreDbContext.SchemaName))
            .Options;

        return new IdentityStoreDbContext(options);
    }
}
