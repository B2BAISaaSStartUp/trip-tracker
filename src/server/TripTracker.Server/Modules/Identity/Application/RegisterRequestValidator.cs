using FluentValidation;
using TripTracker.Identity.Domain;
using TripTracker.SharedKernel.TripTracker.Core.Validation;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Application;

/// <summary>
/// Checks the shape of a sign-up request. The password policy itself (length, digits) is enforced by
/// ASP.NET Core Identity when the account is created.
/// </summary>
internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int EmailMaxLength = 256;
    public const int PasswordMaxLength = 128;

    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithError(Errors.Validation.ValueIsRequired("Email"))
            .MaximumLength(EmailMaxLength).WithError(Errors.Validation.ValueTooLong("Email", EmailMaxLength))
            .EmailAddress().WithError(Errors.Validation.ValueIsInvalid("Email", "not a valid e-mail address"));

        RuleFor(r => r.Password)
            .NotEmpty().WithError(Errors.Validation.ValueIsRequired("Password"))
            .MaximumLength(PasswordMaxLength).WithError(Errors.Validation.ValueTooLong("Password", PasswordMaxLength));

        RuleFor(r => r.DisplayName)
            .NotEmpty().WithError(Errors.Validation.ValueIsRequired("DisplayName"))
            .MaximumLength(AppUser.DisplayNameMaxLength)
            .WithError(Errors.Validation.ValueTooLong("DisplayName", AppUser.DisplayNameMaxLength));
    }
}
