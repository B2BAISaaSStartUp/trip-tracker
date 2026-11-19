using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.SharedKernel.TripTracker.Core.Validation;

public static class ResultConversionExtensions
{
    public static UnitResult<ErrorList> ToErrorList(this UnitResult<Error> res) =>
        res.IsSuccess ? UnitResult.Success<ErrorList>() : res.Error.ToErrorList();

    public static Result<T, ErrorList> ToErrorList<T>(this Result<T, Error> res) =>
        res.IsSuccess ? res.Value : res.Error.ToErrorList();
}
