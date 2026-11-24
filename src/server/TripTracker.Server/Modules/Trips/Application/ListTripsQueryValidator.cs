using FluentValidation;
using TripTracker.SharedKernel.TripTracker.Core.Validation;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;

namespace TripTracker.Trips.Application;

internal sealed class ListTripsQueryValidator : AbstractValidator<ListTripsQuery>
{
    public ListTripsQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1)
            .WithError(Errors.Validation.ValueTooSmall("Page", 1));

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, ListTripsQuery.MaxPageSize)
            .WithError(Errors.Validation.ValueIsInvalid("PageSize", $"must be between 1 and {ListTripsQuery.MaxPageSize}"));
    }
}
