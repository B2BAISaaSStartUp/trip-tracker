namespace TripTracker.Web.Services;

/// <summary>Where the REST API is, read from the <c>Api</c> section of <c>wwwroot/appsettings.json</c>.</summary>
public sealed record ApiSettings(Uri BaseUrl)
{
    /// <summary>Configuration key of the API base address.</summary>
    public const string BaseUrlKey = "Api:BaseUrl";

    /// <summary>Reads and checks the settings. The returned address always ends with a slash.</summary>
    /// <exception cref="InvalidOperationException">The address is missing or is not an absolute http or https URL.</exception>
    public static ApiSettings FromConfiguration(IConfiguration configuration)
    {
        var value = configuration[BaseUrlKey];
        if (!Uri.TryCreate(value, UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"'{BaseUrlKey}' must be an absolute http or https URL.");
        }

        // Without the slash, relative routes would replace the last segment of a base path.
        return new ApiSettings(url.AbsoluteUri.EndsWith('/') ? url : new Uri(url.AbsoluteUri + "/"));
    }
}
