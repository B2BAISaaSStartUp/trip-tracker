using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TripTracker.Markers.Application;
using TripTracker.Markers.Contracts;
using TripTracker.Markers.Persistence;

namespace TripTracker.Markers;

/// <summary>Registers the Markers module.</summary>
public static class MarkersModule
{
    /// <summary>Name of the connection string used by the Markers module.</summary>
    public const string ConnectionStringName = "Markers";

    /// <summary>
    /// Registers Markers services. Reads <c>ConnectionStrings:Markers</c> from configuration
    /// (user secrets or environment variables in development; never commit it). The host must also register
    /// <c>ICurrentUser</c>, and may register <see cref="IMarkerDeletionHook"/> implementations.
    /// </summary>
    /// <exception cref="InvalidOperationException">The connection string is not configured.</exception>
    public static IServiceCollection AddMarkersModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<MarkersDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMarkerRepository, MarkerRepository>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<MarkerService>();
        services.AddScoped<IMarkerService>(provider => provider.GetRequiredService<MarkerService>());
        services.AddScoped<IMarkerTripCleanup>(provider => provider.GetRequiredService<MarkerService>());

        return services;
    }
}
