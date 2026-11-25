using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace TripTracker.Web.Services;

/// <summary>
/// Builds the signed-in user from the claims of the stored access token, and ends the session when the
/// token expires.
/// </summary>
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IAuthSession, IDisposable
{
    // Claim names exactly as the API issues them.
    private const string NameClaim = "name";
    private const string RoleClaim = "role";

    private static readonly TimeSpan MaxTimerDelay = TimeSpan.FromDays(1);
    private static readonly TimeSpan TimerMargin = TimeSpan.FromSeconds(1);

    private readonly ITokenStore _store;
    private readonly TimeProvider _clock;
    private ITimer? _expiryTimer;

    /// <summary>Creates the provider.</summary>
    public JwtAuthenticationStateProvider(ITokenStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <inheritdoc />
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _store.GetAsync();
        var content = ReadUsable(token);
        if (content is null)
        {
            StopExpiryTimer();
            if (token is not null)
            {
                await _store.RemoveAsync();
            }

            return Anonymous();
        }

        StartExpiryTimer(content.ExpiresAt);
        var identity = new ClaimsIdentity(content.Claims, "jwt", NameClaim, RoleClaim);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync()
    {
        var token = await _store.GetAsync();
        if (token is null || ReadUsable(token) is not null)
        {
            return token;
        }

        await SignOutAsync();
        return null;
    }

    /// <inheritdoc />
    public async Task<bool> SignInAsync(string accessToken)
    {
        if (ReadUsable(accessToken) is null)
        {
            return false;
        }

        await _store.SetAsync(accessToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return true;
    }

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        StopExpiryTimer();
        await _store.RemoveAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
    }

    /// <inheritdoc />
    public void Dispose() => StopExpiryTimer();

    private static AuthenticationState Anonymous() => new(new ClaimsPrincipal(new ClaimsIdentity()));

    private JwtContent? ReadUsable(string? token) =>
        token is not null && JwtReader.Read(token) is { } content && content.ExpiresAt > _clock.GetUtcNow()
            ? content
            : null;

    private void StartExpiryTimer(DateTimeOffset expiresAt)
    {
        StopExpiryTimer();

        var delay = expiresAt - _clock.GetUtcNow() + TimerMargin;
        _expiryTimer = _clock.CreateTimer(
            _ => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync()),
            null,
            delay < MaxTimerDelay ? delay : MaxTimerDelay,
            Timeout.InfiniteTimeSpan);
    }

    private void StopExpiryTimer()
    {
        _expiryTimer?.Dispose();
        _expiryTimer = null;
    }
}
