using Microsoft.AspNetCore.Components.Authorization;
using TripTracker.Web.Services;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Services;

public sealed class JwtAuthenticationStateProviderTests : IDisposable
{
    private readonly MemoryTokenStore _store = new();
    private readonly FakeClock _clock = new();
    private readonly JwtAuthenticationStateProvider _provider;
    private readonly List<Task<AuthenticationState>> _notifications = [];

    public JwtAuthenticationStateProviderTests()
    {
        _provider = new JwtAuthenticationStateProvider(_store, _clock);
        _provider.AuthenticationStateChanged += _notifications.Add;
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task State_NoToken_IsAnonymous()
    {
        var state = await _provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task State_ValidToken_HasNameAndRoleFromTheClaims()
    {
        _store.Token = TokenExpiringIn(TimeSpan.FromHours(1), role: "Admin");

        var user = (await _provider.GetAuthenticationStateAsync()).User;

        Assert.True(user.Identity!.IsAuthenticated);
        Assert.Equal("Ann", user.Identity.Name);
        Assert.True(user.IsInRole("Admin"));
        Assert.False(user.IsInRole("User"));
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(0)]
    public async Task State_ExpiredToken_IsAnonymousAndTheTokenIsRemoved(int secondsLeft)
    {
        _store.Token = TokenExpiringIn(TimeSpan.FromSeconds(secondsLeft));

        var state = await _provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
        Assert.Null(_store.Token);
    }

    [Fact]
    public async Task State_UnreadableToken_IsAnonymousAndTheTokenIsRemoved()
    {
        _store.Token = "garbage";

        var state = await _provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
        Assert.Null(_store.Token);
    }

    [Fact]
    public async Task AccessToken_StillValid_IsReturned()
    {
        var token = TokenExpiringIn(TimeSpan.FromSeconds(1));
        _store.Token = token;

        Assert.Equal(token, await _provider.GetAccessTokenAsync());
        Assert.Empty(_notifications);
    }

    [Fact]
    public async Task AccessToken_Expired_EndsTheSession()
    {
        _store.Token = TokenExpiringIn(TimeSpan.FromMinutes(5));
        _clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Null(await _provider.GetAccessTokenAsync());
        Assert.Null(_store.Token);
        Assert.False((await Assert.Single(_notifications)).User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task SignIn_ValidToken_StoresItAndNotifies()
    {
        var token = TokenExpiringIn(TimeSpan.FromHours(1));

        Assert.True(await _provider.SignInAsync(token));

        Assert.Equal(token, _store.Token);
        Assert.True((await Assert.Single(_notifications)).User.Identity!.IsAuthenticated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public async Task SignIn_ExpiredOrUnreadableToken_ChangesNothing(string? token)
    {
        token ??= TokenExpiringIn(TimeSpan.FromSeconds(-1));

        Assert.False(await _provider.SignInAsync(token));

        Assert.Null(_store.Token);
        Assert.Empty(_notifications);
    }

    [Fact]
    public async Task SignOut_RemovesTheTokenNotifiesAndStopsTheTimer()
    {
        await _provider.SignInAsync(TokenExpiringIn(TimeSpan.FromHours(1)));

        await _provider.SignOutAsync();

        Assert.Null(_store.Token);
        Assert.False((await _notifications[^1]).User.Identity!.IsAuthenticated);
        Assert.Empty(_clock.PendingDelays);
    }

    [Fact]
    public async Task Timer_WhenTheTokenExpires_NotifiesThatTheUserIsSignedOut()
    {
        await _provider.SignInAsync(TokenExpiringIn(TimeSpan.FromMinutes(60)));
        _notifications.Clear();

        _clock.Advance(TimeSpan.FromMinutes(59));
        Assert.Empty(_notifications);

        _clock.Advance(TimeSpan.FromMinutes(2));

        Assert.False((await Assert.Single(_notifications)).User.Identity!.IsAuthenticated);
        Assert.Null(_store.Token);
    }

    [Fact]
    public async Task Timer_ForATokenLastingDays_WaitsAtMostADayAndKeepsTheSession()
    {
        await _provider.SignInAsync(TokenExpiringIn(TimeSpan.FromDays(100)));
        Assert.Equal(TimeSpan.FromDays(1), Assert.Single(_clock.PendingDelays));
        _notifications.Clear();

        _clock.Advance(TimeSpan.FromDays(1));

        Assert.True((await Assert.Single(_notifications)).User.Identity!.IsAuthenticated);
        Assert.NotNull(_store.Token);
        Assert.Single(_clock.PendingDelays);
    }

    private string TokenExpiringIn(TimeSpan lifetime, string role = "User") =>
        TestTokens.Create(_clock.GetUtcNow() + lifetime, role: role);
}
