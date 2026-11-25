using System.Text;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TripTracker.Api.Cors;

namespace TripTracker.Api.Tests.Cors;

public class WebClientCorsTests
{
    private const string DevOrigin = "http://localhost:5200";

    [Fact]
    public void Policy_ConfiguredOrigins_AllowsOnlyThoseWithoutCredentials()
    {
        var policy = Policy(Origins(DevOrigin, "https://app.example.com"));

        Assert.Equal([DevOrigin, "https://app.example.com"], policy.Origins);
        Assert.False(policy.AllowAnyOrigin);
        Assert.True(policy.AllowAnyHeader);
        Assert.True(policy.AllowAnyMethod);
        Assert.False(policy.SupportsCredentials);
        Assert.Equal(TimeSpan.FromHours(1), policy.PreflightMaxAge);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{ "Cors": {} }""")]
    [InlineData("""{ "Cors": { "AllowedOrigins": [] } }""")]
    [InlineData("""{ "Cors": { "AllowedOrigins": null } }""")]
    public void Policy_MissingOrEmptyList_AllowsNoOrigin(string json)
    {
        var configuration = Json(json);

        var policy = Policy(configuration);
        var preflight = Preflight(configuration, DevOrigin);

        Assert.Empty(policy.Origins);
        Assert.False(policy.AllowAnyOrigin);
        Assert.False(preflight.IsOriginAllowed);
    }

    [Fact]
    public void Preflight_AllowedOrigin_AllowsTheMethodAndTheClientsHeaders()
    {
        var result = Preflight(Origins(DevOrigin), DevOrigin);

        Assert.True(result.IsPreflightRequest);
        Assert.True(result.IsOriginAllowed);
        Assert.Equal(DevOrigin, result.AllowedOrigin);
        Assert.Contains("GET", result.AllowedMethods);
        Assert.Contains("authorization", result.AllowedHeaders);
        Assert.Contains("content-type", result.AllowedHeaders);
        Assert.False(result.SupportsCredentials);
    }

    [Theory]
    [InlineData("http://evil.example.com")]
    [InlineData("https://localhost:5200")]
    [InlineData("http://localhost:5201")]
    [InlineData("http://localhost.evil.example.com:5200")]
    [InlineData("null")]
    public void Preflight_OtherOrigin_IsNotAllowed(string origin)
    {
        var result = Preflight(Origins(DevOrigin), origin);

        Assert.False(result.IsOriginAllowed);
    }

    [Theory]
    [InlineData("http://localhost:5200", "http://localhost:5200")]
    [InlineData("HTTPS://App.Example.COM", "https://app.example.com")]
    [InlineData("http://[::1]:5200", "http://[::1]:5200")]
    [InlineData("http://192.168.0.10:8080", "http://192.168.0.10:8080")]
    public void Preflight_ValidConfiguredOrigin_MatchesWhatABrowserSends(string configured, string sentByBrowser)
    {
        var result = Preflight(Origins(configured), sentByBrowser);

        Assert.True(result.IsOriginAllowed);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData("http://localhost:*")]
    public void Configuration_Wildcard_IsRejected(string entry)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Register(Origins(entry)));

        Assert.Contains("wildcards are not allowed", exception.Message, StringComparison.Ordinal);
        Assert.Contains(WebClientCors.AllowedOriginsKey, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("null")]
    [InlineData("localhost:5200")]
    [InlineData("localhost")]
    [InlineData("/app")]
    [InlineData("http://")]
    [InlineData("ftp://example.com")]
    [InlineData("file:///c:/site")]
    [InlineData("http://localhost:5200/")]
    [InlineData("http://localhost:5200/app")]
    [InlineData("http://localhost:5200?a=1")]
    [InlineData("http://localhost:5200#a")]
    [InlineData("http://localhost:80")]
    [InlineData("https://example.com:443")]
    [InlineData("http://user@example.com")]
    [InlineData(" http://localhost:5200")]
    [InlineData("http://localhost:5200 ")]
    [InlineData("http://localhost:99999")]
    [InlineData("http://a.example.com,http://b.example.com")]
    [InlineData("http://a.example.com;http://b.example.com")]
    public void Configuration_MalformedEntry_IsRejectedAndNamed(string entry)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Register(Origins(DevOrigin, entry)));

        Assert.Contains("'Cors:AllowedOrigins:1'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "Cors": { "AllowedOrigins": "http://localhost:5200" } }""")]
    [InlineData("""{ "Cors": { "AllowedOrigins": "*" } }""")]
    [InlineData("""{ "Cors": { "AllowedOrigins": [ { "url": "http://localhost:5200" } ] } }""")]
    [InlineData("""{ "Cors": { "AllowedOrigins": [ null ] } }""")]
    public void Configuration_NotAListOfText_IsRejected(string json)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Register(Json(json)));

        Assert.Contains(WebClientCors.AllowedOriginsKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShippedSettings_WithoutTheDevelopmentFile_AllowNoOrigin()
    {
        var configuration = ShippedSettings("appsettings.json");

        Assert.Empty(Policy(configuration).Origins);
    }

    [Fact]
    public void ShippedSettings_InDevelopment_AllowTheWebDevServer()
    {
        var configuration = ShippedSettings("appsettings.json", "appsettings.Development.json");

        // The addresses in src/web/TripTracker.Web/Properties/launchSettings.json.
        Assert.Equal([DevOrigin, "https://localhost:7200"], Policy(configuration).Origins);
    }

    private static IConfiguration Origins(params string[] origins) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(origins.Select((origin, index) =>
                KeyValuePair.Create($"{WebClientCors.AllowedOriginsKey}:{index}", (string?)origin)))
            .Build();

    private static IConfiguration Json(string json) =>
        new ConfigurationBuilder().AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json))).Build();

    // The Api's settings files are copied next to the tests because the test project references the Api.
    private static IConfiguration ShippedSettings(params string[] files)
    {
        var builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory);
        foreach (var file in files)
        {
            builder.AddJsonFile(file, optional: false);
        }

        return builder.Build();
    }

    private static ServiceProvider Register(IConfiguration configuration) =>
        new ServiceCollection().AddLogging().AddWebClientCors(configuration).BuildServiceProvider();

    private static CorsPolicy Policy(IConfiguration configuration)
    {
        using var services = Register(configuration);
        return services.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(WebClientCors.PolicyName)!;
    }

    private static CorsResult Preflight(IConfiguration configuration, string origin)
    {
        using var services = Register(configuration);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Options;
        context.Request.Headers.Origin = origin;
        context.Request.Headers.AccessControlRequestMethod = HttpMethods.Get;
        context.Request.Headers.AccessControlRequestHeaders = "authorization,content-type";

        var policy = services.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy(WebClientCors.PolicyName)!;
        return services.GetRequiredService<ICorsService>().EvaluatePolicy(context, policy);
    }
}
