using Microsoft.Extensions.Configuration;
using TripTracker.Web.Services;

namespace TripTracker.Web.Tests.Services;

public class ApiSettingsTests
{
    [Theory]
    [InlineData("http://localhost:5183", "http://localhost:5183/")]
    [InlineData("https://example.com/backend", "https://example.com/backend/")]
    [InlineData("https://example.com/backend/", "https://example.com/backend/")]
    public void FromConfiguration_HttpAddress_EndsWithASlash(string configured, string expected)
    {
        var settings = ApiSettings.FromConfiguration(Configuration(configured));

        Assert.Equal(expected, settings.BaseUrl.AbsoluteUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("localhost:5183")]
    [InlineData("/api")]
    [InlineData("ftp://example.com")]
    public void FromConfiguration_MissingOrNotHttp_Throws(string? configured)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ApiSettings.FromConfiguration(Configuration(configured)));

        Assert.Contains(ApiSettings.BaseUrlKey, exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration Configuration(string? baseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ApiSettings.BaseUrlKey] = baseUrl })
            .Build();
}
