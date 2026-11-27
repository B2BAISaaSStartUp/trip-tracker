using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripTracker.Markers.Contracts;
using TripTracker.Markers.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Core;

namespace TripTracker.Markers.Tests;

public class MarkersModuleTests
{
    private static IConfiguration Config(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(connectionString is null
                ? []
                : new Dictionary<string, string?> { [$"ConnectionStrings:{MarkersModule.ConnectionStringName}"] = connectionString })
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void AddMarkersModule_WithoutConnectionString_Throws(string? connectionString)
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() => services.AddMarkersModule(Config(connectionString)));

        Assert.Contains("ConnectionStrings:Markers", error.Message);
    }

    [Fact]
    public void AddMarkersModule_RegistersTheContracts_AsOneScopedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddMarkersModule(Config("Host=localhost;Database=offline"));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var markers = scope.ServiceProvider.GetRequiredService<IMarkerService>();
        var cleanup = scope.ServiceProvider.GetRequiredService<IMarkerTripCleanup>();

        Assert.Same(markers, cleanup);
    }
}
