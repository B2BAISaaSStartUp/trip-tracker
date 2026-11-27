using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TripTracker.Tracking.Application;
using TripTracker.Tracking.Contracts;
using TripTracker.Tracking.Persistence;

namespace TripTracker.Tracking;

/// <summary>Registers the Tracking module.</summary>
public static class TrackingModule
{
    /// <summary>Name of the connection string used by the Tracking module.</summary>
    public const string ConnectionStringName = "Tracking";

    /// <summary>
    /// Registers Tracking services. Reads <c>ConnectionStrings:Tracking</c> from configuration
    /// (user secrets or environment variables in development; never commit it). The host must also register
    /// <c>ICurrentUser</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The connection string is not configured.</exception>
    public static IServiceCollection AddTrackingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<TrackingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITrackPointRepository, TrackPointRepository>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<TrackingService>();
        services.AddScoped<ITrackingService>(provider => provider.GetRequiredService<TrackingService>());
        services.AddScoped<ITrackingTripCleanup>(provider => provider.GetRequiredService<TrackingService>());

        return services;
    }
}
