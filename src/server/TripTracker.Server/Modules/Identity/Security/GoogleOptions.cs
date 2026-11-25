namespace TripTracker.Identity.Security;

/// <summary>Settings for Sign in with Google, read from <c>Identity:Google</c>.</summary>
internal sealed class GoogleOptions
{
    /// <summary>OAuth client ids whose ID tokens are accepted (web, Android). Empty means Google sign-in is off.</summary>
    public List<string> ClientIds { get; set; } = [];
}
