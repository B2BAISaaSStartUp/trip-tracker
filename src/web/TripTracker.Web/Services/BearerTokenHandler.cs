using System.Net;
using System.Net.Http.Headers;

namespace TripTracker.Web.Services;

/// <summary>
/// Adds the access token to API requests. A 401 answer ends the session, which sends the user from a
/// protected page to the login page.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IAuthSession _session;

    /// <summary>Creates the handler.</summary>
    public BearerTokenHandler(IAuthSession session)
    {
        _session = session;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _session.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized && !IsCredentialRequest(request))
        {
            await _session.SignOutAsync();
        }

        return response;
    }

    /// <summary>For these a 401 means wrong credentials, not an ended session.</summary>
    private static bool IsCredentialRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        return path.EndsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/api/auth/register", StringComparison.OrdinalIgnoreCase);
    }
}
