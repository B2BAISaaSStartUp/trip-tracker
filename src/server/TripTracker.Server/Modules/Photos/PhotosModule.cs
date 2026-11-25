using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TripTracker.Photos.Application;
using TripTracker.Photos.Contracts;
using TripTracker.Photos.Imaging;
using TripTracker.Photos.Persistence;
using TripTracker.Photos.Storage;

namespace TripTracker.Photos;

/// <summary>Registers the Photos module: photo metadata in PostgreSQL, files in MinIO.</summary>
public static class PhotosModule
{
    /// <summary>Name of the connection string used by the Photos module.</summary>
    public const string ConnectionStringName = "Photos";

    internal const string MigrationsHistoryTable = "__ef_migrations_history";

    private const string StorageSection = "Photos:Storage";

    /// <summary>
    /// Registers Photos services. Reads <c>ConnectionStrings:Photos</c> and <c>Photos:Storage</c> (endpoint,
    /// access key, secret key, bucket) from configuration - user secrets or environment variables in development;
    /// never commit the keys. The host must also register <c>ICurrentUser</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The connection string or the storage settings are missing or invalid.</exception>
    public static IServiceCollection AddPhotosModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
        }

        var storage = configuration.GetSection(StorageSection).Get<PhotoStorageOptions>() ?? new PhotoStorageOptions();
        storage.Validate();

        Register(
            services,
            storage,
            db => db.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, PhotosDbContext.SchemaName)));

        return services;
    }

    /// <summary>Wires everything except the database provider, which tests replace with an in-memory one.</summary>
    internal static void Register(
        IServiceCollection services,
        PhotoStorageOptions storage,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<PhotosDbContext>(configureDatabase);
        services.AddScoped<IPhotoRepository, PhotoRepository>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(Options.Create(storage));
        services.AddSingleton<IObjectStore, S3ObjectStore>();
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();

        services.AddScoped<PhotoService>();
        services.AddScoped<IPhotoService>(provider => provider.GetRequiredService<PhotoService>());
        services.AddScoped<IPhotoTripCleanup>(provider => provider.GetRequiredService<PhotoService>());
    }
}
