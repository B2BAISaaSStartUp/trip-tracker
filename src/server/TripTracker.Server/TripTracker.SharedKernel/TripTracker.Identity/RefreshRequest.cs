namespace TripTracker.SharedKernel.TripTracker.Identity;

/// <summary>Request to exchange a refresh token for a new access token, or to revoke it on sign-out.</summary>
public sealed record RefreshRequest(string RefreshToken);
