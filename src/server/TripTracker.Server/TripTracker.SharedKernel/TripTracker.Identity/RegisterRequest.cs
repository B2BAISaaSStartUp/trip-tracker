namespace TripTracker.SharedKernel.TripTracker.Identity;

/// <summary>Request to create an account with e-mail and password (FR-01).</summary>
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
