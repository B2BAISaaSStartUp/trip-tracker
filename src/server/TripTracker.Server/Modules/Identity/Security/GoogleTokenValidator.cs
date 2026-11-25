using CSharpFunctionalExtensions;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using TripTracker.Identity.Domain;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Security;

/// <summary>Validates Google ID tokens with Google's public keys.</summary>
internal sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly GoogleOptions _options;

    public GoogleTokenValidator(IOptions<GoogleOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Result<GoogleIdentity, Error>> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (_options.ClientIds.Count == 0)
        {
            return IdentityErrors.GoogleNotConfigured();
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = _options.ClientIds };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleIdentity(payload.Subject, payload.Email ?? string.Empty, payload.EmailVerified, payload.Name);
        }
        catch (InvalidJwtException)
        {
            return IdentityErrors.GoogleTokenInvalid();
        }
    }
}
