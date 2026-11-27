using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.Tracking.Contracts;
using TripTracker.Tracking.Tests.Fakes;

namespace TripTracker.Tracking.Tests;

public class TrackingModuleTests
{
    private static IConfiguration Config(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(connectionString is null
                ? []
                : new Dictionary<string, string?> { [$"ConnectionStrings:{TrackingModule.ConnectionStringName}"] = connectionString })
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void AddTrackingModule_WithoutConnectionString_Throws(string? connectionString)
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() => services.AddTrackingModule(Config(connectionString)));

        Assert.Contains("ConnectionStrings:Tracking", error.Message);
    }

    [Fact]
    public void AddTrackingModule_RegistersTheContracts_AsOneScopedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddTrackingModule(Config("Host=localhost;Database=offline"));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var tracking = scope.ServiceProvider.GetRequiredService<ITrackingService>();
        var cleanup = scope.ServiceProvider.GetRequiredService<ITrackingTripCleanup>();

        Assert.Same(tracking, cleanup);
    }
}
