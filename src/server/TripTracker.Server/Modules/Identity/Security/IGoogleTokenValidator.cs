using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Security;

/// <summary>The facts about a person that Google vouches for in an ID token.</summary>
internal sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified, string? Name);

/// <summary>Port to Google OAuth 2.0 / OpenID Connect (IOAuth in the component diagram).</summary>
internal interface IGoogleTokenValidator
{
    /// <summary>Checks the signature, issuer, expiry and audience of a Google ID token.</summary>
    Task<Result<GoogleIdentity, Error>> ValidateAsync(string idToken, CancellationToken ct);
}
