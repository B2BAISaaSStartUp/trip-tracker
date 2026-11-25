using Microsoft.JSInterop;

namespace TripTracker.Web.Services;

/// <summary><see cref="ITokenStore"/> over the browser's <c>localStorage</c>.</summary>
public sealed class LocalStorageTokenStore : ITokenStore
{
    private const string Key = "triptracker.accessToken";

    private readonly IJSRuntime _js;

    /// <summary>Creates the store.</summary>
    public LocalStorageTokenStore(IJSRuntime js)
    {
        _js = js;
    }

    /// <inheritdoc />
    public async Task<string?> GetAsync() => await _js.InvokeAsync<string?>("localStorage.getItem", Key);

    /// <inheritdoc />
    public async Task SetAsync(string token) => await _js.InvokeVoidAsync("localStorage.setItem", Key, token);

    /// <inheritdoc />
    public async Task RemoveAsync() => await _js.InvokeVoidAsync("localStorage.removeItem", Key);
}
