namespace TripTracker.Api.Cors;

/// <summary>
/// CORS for the browser client (NFR-09): web pages may call the API only from the origins listed in
/// <c>Cors:AllowedOrigins</c>. With no origins configured, none is allowed.
/// </summary>
public static class WebClientCors
{
    /// <summary>Name of the CORS policy.</summary>
    public const string PolicyName = "WebClient";

    /// <summary>Configuration key of the list of allowed origins.</summary>
    public const string AllowedOriginsKey = "Cors:AllowedOrigins";

    // How long a browser may reuse a preflight answer. Browsers apply their own upper limit as well.
    private static readonly TimeSpan PreflightMaxAge = TimeSpan.FromHours(1);

    /// <summary>
    /// Registers the policy with the origins from <c>Cors:AllowedOrigins</c>. The host must also call
    /// <c>UseCors</c> with <see cref="PolicyName"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The setting is not a list, or an entry is not an http or https origin.</exception>
    public static IServiceCollection AddWebClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = ReadAllowedOrigins(configuration);

        // No AllowCredentials: the token travels in the Authorization header, not in a cookie.
        return services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetPreflightMaxAge(PreflightMaxAge)));
    }

    private static string[] ReadAllowedOrigins(IConfiguration configuration)
    {
        var section = configuration.GetSection(AllowedOriginsKey);

        // A single value in place of the list would otherwise be ignored without a word.
        if (!string.IsNullOrEmpty(section.Value))
        {
            throw new InvalidOperationException(
                $"'{AllowedOriginsKey}' must be a list of origins, not a single value (set '{AllowedOriginsKey}:0').");
        }

        return section.GetChildren().Select(ReadOrigin).ToArray();
    }

    private static string ReadOrigin(IConfigurationSection entry)
    {
        var value = entry.Value ?? string.Empty;
        if (value.Contains('*'))
        {
            throw new InvalidOperationException(
                $"'{entry.Path}' is '{value}': wildcards are not allowed in '{AllowedOriginsKey}'. List each origin.");
        }

        // A browser sends "scheme://host" or "scheme://host:port", with no default port and no trailing slash.
        // Anything else would never match, so it is rejected instead of silently blocking the client.
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.UserInfo.Length > 0
            || !string.Equals(value, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'{entry.Path}' is '{value}', which is not a valid origin for '{AllowedOriginsKey}'. Use "
                + "http://host[:port] or https://host[:port] without a path, trailing slash or default port.");
        }

        return value;
    }
}
