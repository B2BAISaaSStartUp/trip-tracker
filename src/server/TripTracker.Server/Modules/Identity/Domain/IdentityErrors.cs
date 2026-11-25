using Microsoft.AspNetCore.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Domain;

/// <summary>Errors produced by the Identity module.</summary>
internal static class IdentityErrors
{
    public static Error InvalidCredentials() =>
        Error.Unauthorized("auth.credentials.invalid", "E-mail or password is incorrect");

    public static Error NotAuthenticated() =>
        Error.Unauthorized("auth.user.not.authenticated", "You must be signed in to do this");

    public static Error EmailTaken() =>
        Error.AlreadyExist("auth.email.taken", "An account with this e-mail already exists");

    public static Error AccountBlocked() =>
        Error.Forbidden("auth.account.blocked", "This account is blocked");

    public static Error LockedOut() =>
        Error.TooManyRequests("auth.account.locked", "Too many failed sign-in attempts; try again later");

    public static Error RefreshTokenInvalid() =>
        Error.Unauthorized("auth.refresh.invalid", "The refresh token is invalid or expired");

    public static Error GoogleTokenInvalid() =>
        Error.Unauthorized("auth.google.invalid", "The Google token is invalid");

    public static Error GoogleNotConfigured() =>
        Error.Unauthorized("auth.google.not.configured", "Sign-in with Google is not enabled");

    public static Error GoogleEmailNotVerified() =>
        Error.Unauthorized("auth.google.email.not.verified", "The Google account has no verified e-mail");

    public static Error UserNotFound() =>
        Error.NotFound("auth.user.not.found", "User was not found");

    public static Error AdminCannotBeBlocked() =>
        Error.Conflict("auth.admin.cannot.be.blocked", "An administrator account cannot be blocked");

    /// <summary>Turns the first failure of an ASP.NET Core Identity operation into an <see cref="Error"/>.</summary>
    public static Error FromIdentityResult(IdentityResult result)
    {
        var first = result.Errors.FirstOrDefault();
        if (first is null)
        {
            return Error.Failure("auth.operation.failed", "The operation failed");
        }

        if (first.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName))
        {
            return EmailTaken();
        }

        if (first.Code.StartsWith("Password", StringComparison.Ordinal))
        {
            return Error.Validation("auth.password.weak", first.Description, "Password");
        }

        return Error.Validation("auth.request.invalid", first.Description);
    }
}
