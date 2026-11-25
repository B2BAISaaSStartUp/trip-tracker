using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TripTracker.Photos.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c>. Reads the connection string from the <c>ConnectionStrings__Photos</c>
/// environment variable, so no credentials live in the repository.
/// </summary>
internal sealed class PhotosDbContextFactory : IDesignTimeDbContextFactory<PhotosDbContext>
{
    public PhotosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Photos");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set the ConnectionStrings__Photos environment variable before running dotnet ef.");
        }

        var options = new DbContextOptionsBuilder<PhotosDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                PhotosModule.MigrationsHistoryTable,
                PhotosDbContext.SchemaName))
            .Options;

        return new PhotosDbContext(options);
    }
}
