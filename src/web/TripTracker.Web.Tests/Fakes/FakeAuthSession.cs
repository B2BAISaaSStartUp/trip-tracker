using TripTracker.Web.Services;

namespace TripTracker.Web.Tests.Fakes;

internal sealed class FakeAuthSession : IAuthSession
{
    public bool AcceptsTokens { get; set; } = true;

    public List<string> SignIns { get; } = [];

    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>(null);

    public Task<bool> SignInAsync(string accessToken)
    {
        SignIns.Add(accessToken);
        return Task.FromResult(AcceptsTokens);
    }

    public Task SignOutAsync() => Task.CompletedTask;
}
