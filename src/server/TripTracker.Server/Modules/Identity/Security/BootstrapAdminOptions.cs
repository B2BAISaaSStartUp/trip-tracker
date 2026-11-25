namespace TripTracker.Identity.Security;

/// <summary>
/// Optional first administrator, read from <c>Identity:BootstrapAdmin</c>. Created at start-up when no account
/// with this e-mail exists. Supply the password through user secrets or an environment variable.
/// </summary>
internal sealed class BootstrapAdminOptions
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string DisplayName { get; set; } = "Administrator";
}
