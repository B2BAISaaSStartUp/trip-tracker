using CSharpFunctionalExtensions;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Security;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Tests.Fakes;

/// <summary>Accepts the token "valid" and returns whatever identity the test set; refuses everything else.</summary>
internal sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public const string ValidToken = "valid";

    public GoogleIdentity Identity { get; set; } = new("google-sub-1", "traveller@example.com", true, "Traveller");

    public Task<Result<GoogleIdentity, Error>> ValidateAsync(string idToken, CancellationToken ct)
    {
        Result<GoogleIdentity, Error> result = idToken == ValidToken
            ? Identity
            : IdentityErrors.GoogleTokenInvalid();

        return Task.FromResult(result);
    }
}
