namespace TripTracker.SharedKernel.TripTracker.Identity;

/// <summary>Request to sign in with e-mail and password (FR-02).</summary>
public sealed record LoginRequest(string Email, string Password);
