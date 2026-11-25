using TripTracker.Web.Services;

namespace TripTracker.Web.Tests.Fakes;

internal sealed class MemoryTokenStore : ITokenStore
{
    public string? Token { get; set; }

    public Task<string?> GetAsync() => Task.FromResult(Token);

    public Task SetAsync(string token)
    {
        Token = token;
        return Task.CompletedTask;
    }

    public Task RemoveAsync()
    {
        Token = null;
        return Task.CompletedTask;
    }
}
