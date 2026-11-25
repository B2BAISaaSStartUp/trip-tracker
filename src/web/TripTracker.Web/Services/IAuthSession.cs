namespace TripTracker.Web.Services;

/// <summary>
/// The sign-in state of this browser. Only the access token is kept: the client discards the refresh token,
/// so the user signs in again when the access token expires.
/// </summary>
public interface IAuthSession
{
    /// <summary>The token to send to the API, or <c>null</c> when signed out. An expired token ends the session.</summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>Starts a session. Returns <c>false</c> and changes nothing if the token is unreadable or already expired.</summary>
    Task<bool> SignInAsync(string accessToken);

    /// <summary>Ends the session in this browser. Nothing is revoked on the server.</summary>
    Task SignOutAsync();
}
