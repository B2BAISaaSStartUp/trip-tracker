using System.Net;
using TripTracker.Web.Services;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Services;

public sealed class BearerTokenHandlerTests : IDisposable
{
    private readonly MemoryTokenStore _store = new();
    private readonly JwtAuthenticationStateProvider _session;

    public BearerTokenHandlerTests()
    {
        _session = new JwtAuthenticationStateProvider(_store, new FakeClock());
        _store.Token = TestTokens.Create(new FakeClock().GetUtcNow().AddHours(1));
    }

    public void Dispose() => _session.Dispose();

    [Fact]
    public async Task Send_SignedIn_AddsTheBearerHeader()
    {
        var api = StubHttpMessageHandler.Responding(HttpStatusCode.OK);

        await GetAsync(api, "api/trips");

        Assert.Equal($"Bearer {_store.Token}", api.Authorization);
    }

    [Fact]
    public async Task Send_SignedOut_SendsNoAuthorizationHeader()
    {
        _store.Token = null;
        var api = StubHttpMessageHandler.Responding(HttpStatusCode.OK);

        await GetAsync(api, "api/trips");

        Assert.Null(api.Authorization);
    }

    [Fact]
    public async Task Send_Unauthorized_EndsTheSession()
    {
        await GetAsync(StubHttpMessageHandler.Responding(HttpStatusCode.Unauthorized), "api/trips");

        Assert.Null(_store.Token);
    }

    [Theory]
    [InlineData("api/auth/login")]
    [InlineData("api/auth/register")]
    public async Task Send_UnauthorizedForCredentials_KeepsTheSession(string route)
    {
        await GetAsync(StubHttpMessageHandler.Responding(HttpStatusCode.Unauthorized), route);

        Assert.NotNull(_store.Token);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Send_OtherFailure_KeepsTheSession(HttpStatusCode status)
    {
        await GetAsync(StubHttpMessageHandler.Responding(status), "api/trips");

        Assert.NotNull(_store.Token);
    }

    private async Task GetAsync(HttpMessageHandler api, string route)
    {
        using var http = new HttpClient(new BearerTokenHandler(_session) { InnerHandler = api })
        {
            BaseAddress = new Uri("http://api.test/"),
        };
        using var response = await http.GetAsync(route);
    }
}
