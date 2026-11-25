namespace TripTracker.Web.Services;

/// <summary>Keeps the access token between page loads.</summary>
public interface ITokenStore
{
    /// <summary>The stored token, or <c>null</c> if there is none.</summary>
    Task<string?> GetAsync();

    /// <summary>Stores a token, replacing any earlier one.</summary>
    Task SetAsync(string token);

    /// <summary>Forgets the stored token.</summary>
    Task RemoveAsync();
}
