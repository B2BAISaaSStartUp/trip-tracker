using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TripTracker.Identity.Application;
using TripTracker.Identity.Contracts;
using TripTracker.Identity.Security;
using TripTracker.Identity.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Framework;

namespace TripTracker.Identity.Tests;

public class IdentityModuleTests
{
    private static readonly string SigningKey = new('k', 48);

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> ValidSettings() =>
        new()
        {
            [$"ConnectionStrings:{IdentityModule.ConnectionStringName}"] = "Host=localhost;Database=offline",
            ["Identity:Jwt:SigningKey"] = SigningKey,
        };

    private static ServiceProvider Build(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser());
        services.AddIdentityModule(configuration);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void AddIdentityModule_WithoutConnectionString_Throws(string? connectionString)
    {
        var settings = ValidSettings();
        settings[$"ConnectionStrings:{IdentityModule.ConnectionStringName}"] = connectionString;

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddIdentityModule(Config(settings)));

        Assert.Contains("ConnectionStrings:Identity", error.Message);
    }

    [Fact]
    public void AddIdentityModule_WithoutSigningKey_Throws()
    {
        var settings = ValidSettings();
        settings.Remove("Identity:Jwt:SigningKey");

        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddIdentityModule(Config(settings)));

        Assert.Contains("SigningKey", error.Message);
    }

    [Fact]
    public void AddIdentityModule_RegistersTheContracts()
    {
        using var provider = Build(Config(ValidSettings()));
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuthService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserManagement>());
    }

    [Fact]
    public void AddIdentityModule_ConfiguresJwtBearerFromTheSettings()
    {
        var settings = ValidSettings();
        settings["Identity:Jwt:Issuer"] = "my-issuer";
        using var provider = Build(Config(settings));

        var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal("my-issuer", bearer.TokenValidationParameters.ValidIssuer);
        Assert.True(bearer.TokenValidationParameters.ValidateLifetime);
        Assert.True(bearer.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.Equal("role", bearer.TokenValidationParameters.RoleClaimType);
        Assert.False(bearer.MapInboundClaims);
    }

    [Fact]
    public void AddIdentityModule_WithoutBootstrapSettings_RegistersNoSeeder()
    {
        using var provider = Build(Config(ValidSettings()));

        Assert.DoesNotContain(provider.GetServices<IHostedService>(), s => s is BootstrapAdminService);
    }

    [Fact]
    public void AddIdentityModule_WithBootstrapSettings_RegistersTheSeeder()
    {
        var settings = ValidSettings();
        settings["Identity:BootstrapAdmin:Email"] = "admin@example.com";
        settings["Identity:BootstrapAdmin:Password"] = "Passw0rd";
        using var provider = Build(Config(settings));

        Assert.Contains(provider.GetServices<IHostedService>(), s => s is BootstrapAdminService);
    }

    [Fact]
    public async Task BootstrapAdmin_CreatesAnAdministrator_OnlyOnce()
    {
        using var bed = new IdentityTestBed();
        var options = Options.Create(new BootstrapAdminOptions { Email = "admin@example.com", Password = "Passw0rd" });
        var seeder = new BootstrapAdminService(bed.Scopes, options, bed.Clock, NullLogger<BootstrapAdminService>.Instance);

        await seeder.StartAsync(CancellationToken.None);
        await seeder.StartAsync(CancellationToken.None);

        var admin = await bed.Manager.FindByEmailAsync("admin@example.com");
        Assert.NotNull(admin);
        Assert.Equal(RoleNames.ADMIN, admin.Role);

        var login = await bed.Auth.LoginAsync(new(admin.Email!, "Passw0rd"), CancellationToken.None);
        Assert.True(login.IsSuccess);
        Assert.Equal(RoleNames.ADMIN, login.Value.User.Role);
    }
}
