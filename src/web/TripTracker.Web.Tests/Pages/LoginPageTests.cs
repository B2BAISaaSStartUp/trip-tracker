using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Web.Models;
using TripTracker.Web.Pages;
using TripTracker.Web.Services;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Pages;

public sealed class LoginPageTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();
    private readonly FakeApiClient _api = new();
    private readonly FakeAuthSession _session = new();

    public LoginPageTests()
    {
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.Services.AddMudServices();
        _context.Services.AddSingleton<IApiClient>(_api);
        _context.Services.AddSingleton<IAuthSession>(_session);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // MudBlazor registers services that can only be disposed asynchronously.
    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public void Login_EmptyForm_ShowsRequiredMessagesWithoutCallingTheApi()
    {
        var page = _context.Render<Login>();

        Submit(page);

        page.WaitForAssertion(() =>
        {
            Assert.Contains("E-mail is required", page.Markup, StringComparison.Ordinal);
            Assert.Contains("Password is required", page.Markup, StringComparison.Ordinal);
        });
        Assert.Empty(_api.Logins);
    }

    [Fact]
    public void Login_Success_SignsInWithTheAccessTokenAndOpensTrips()
    {
        var page = _context.Render<Login>();
        Fill(page, "  ann@example.com  ", "secret-1");

        Submit(page);

        var navigation = _context.Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.EndsWith("/trips", navigation.Uri, StringComparison.Ordinal));
        Assert.Equal(new LoginRequest("ann@example.com", "secret-1"), Assert.Single(_api.Logins));
        Assert.Equal("access-token", Assert.Single(_session.SignIns));
    }

    [Fact]
    public void Login_ApiError_ShowsItsMessageAndDoesNotSignIn()
    {
        _api.AuthResult = ApiResult<AuthResponse>.Failure(
            [new ApiError("auth.credentials.invalid", "E-mail or password is incorrect", ErrorType.UNAUTHORIZED)]);
        var page = _context.Render<Login>();
        Fill(page, "ann@example.com", "wrong");

        Submit(page);

        page.WaitForAssertion(() =>
            Assert.Contains("E-mail or password is incorrect", page.Find("[role=alert]").TextContent, StringComparison.Ordinal));
        Assert.Empty(_session.SignIns);
    }

    [Fact]
    public void Login_TokenTheSessionRejects_ShowsWhySignInFailed()
    {
        _session.AcceptsTokens = false;
        var page = _context.Render<Login>();
        Fill(page, "ann@example.com", "secret-1");

        Submit(page);

        page.WaitForAssertion(() =>
            Assert.Contains("date and time", page.Find("[role=alert]").TextContent, StringComparison.Ordinal));
    }

    private static void Fill(IRenderedComponent<Login> page, string email, string password)
    {
        page.Find("input[type=email]").Change(email);
        page.Find("input[type=password]").Change(password);
    }

    private static void Submit(IRenderedComponent<Login> page) =>
        page.FindAll("button").Single(b => b.TextContent.Trim() == "Log in").Click();
}
