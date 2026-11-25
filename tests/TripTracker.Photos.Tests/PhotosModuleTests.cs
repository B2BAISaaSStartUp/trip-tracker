using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripTracker.Photos.Contracts;
using TripTracker.Photos.Storage;
using TripTracker.Photos.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Core;

namespace TripTracker.Photos.Tests;

public class PhotosModuleTests
{
    private static Dictionary<string, string?> ValidSettings() =>
        new()
        {
            [$"ConnectionStrings:{PhotosModule.ConnectionStringName}"] = "Host=localhost;Database=offline",
            ["Photos:Storage:Endpoint"] = "http://localhost:9000",
            ["Photos:Storage:AccessKey"] = "test-access-key",
            ["Photos:Storage:SecretKey"] = "test-secret-key",
        };

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void AddPhotosModule_WithoutConnectionString_Throws(string? connectionString)
    {
        var settings = ValidSettings();
        settings[$"ConnectionStrings:{PhotosModule.ConnectionStringName}"] = connectionString;

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPhotosModule(Config(settings)));

        Assert.Contains("ConnectionStrings:Photos", error.Message);
    }

    [Theory]
    [InlineData("Photos:Storage:Endpoint")]
    [InlineData("Photos:Storage:AccessKey")]
    [InlineData("Photos:Storage:SecretKey")]
    public void AddPhotosModule_WithoutAStorageSetting_Throws(string missing)
    {
        var settings = ValidSettings();
        settings.Remove(missing);

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPhotosModule(Config(settings)));

        Assert.Contains(missing.Split(':').Last(), error.Message);
    }

    [Fact]
    public void AddPhotosModule_WithAnEndpointThatIsNotAUrl_Throws()
    {
        var settings = ValidSettings();
        settings["Photos:Storage:Endpoint"] = "minio";

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPhotosModule(Config(settings)));
    }

    [Fact]
    public void AddPhotosModule_RegistersTheContracts_AndOneServiceBehindBoth()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddPhotosModule(Config(ValidSettings()));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var photos = scope.ServiceProvider.GetRequiredService<IPhotoService>();
        var cleanup = scope.ServiceProvider.GetRequiredService<IPhotoTripCleanup>();

        Assert.Same(photos, cleanup);
    }

    [Fact]
    public void AddPhotosModule_UsesTheS3Store()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddPhotosModule(Config(ValidSettings()));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<S3ObjectStore>(provider.GetRequiredService<IObjectStore>());
    }
}
