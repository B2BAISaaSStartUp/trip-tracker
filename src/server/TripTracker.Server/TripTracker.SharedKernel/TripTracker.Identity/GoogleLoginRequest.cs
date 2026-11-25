namespace TripTracker.SharedKernel.TripTracker.Identity;

/// <summary>Request to sign in with Google (FR-04). <c>IdToken</c> is the OpenID Connect ID token obtained by the client.</summary>
public sealed record GoogleLoginRequest(string IdToken);
