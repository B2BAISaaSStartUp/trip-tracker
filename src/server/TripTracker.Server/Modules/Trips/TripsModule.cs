using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripTracker.Trips.Application;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Persistence;

namespace TripTracker.Trips;

/// <summary>Registers the Trips module.</summary>
public static class TripsModule
{
    /// <summary>Name of the connection string used by the Trips module.</summary>
    public const string ConnectionStringName = "Trips";

    /// <summary>
    /// Registers Trips services. Reads <c>ConnectionStrings:Trips</c> from configuration
    /// (user secrets or environment variables in development; never commit it).
    /// </summary>
    /// <exception cref="InvalidOperationException">The connection string is not configured.</exception>
    public static IServiceCollection AddTripsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
        }

        services.AddDbContext<TripsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITripRepository, TripRepository>();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITripAccess, TripAccessService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<ITripModeration, TripModerationService>();

        return services;
    }
}
