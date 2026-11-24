using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.Trips.Contracts;
using TripTracker.Trips.Tests.Fakes;

namespace TripTracker.Trips.Tests;

public class TripsModuleTests
{
    private static IConfiguration Config(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(connectionString is null
                ? []
                : new Dictionary<string, string?> { [$"ConnectionStrings:{TripsModule.ConnectionStringName}"] = connectionString })
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void AddTripsModule_WithoutConnectionString_Throws(string? connectionString)
    {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() => services.AddTripsModule(Config(connectionString)));

        Assert.Contains("ConnectionStrings:Trips", error.Message);
    }

    [Fact]
    public void AddTripsModule_RegistersTheThreeContracts()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddTripsModule(Config("Host=localhost;Database=offline"));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripAccess>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripModeration>());
    }
}
