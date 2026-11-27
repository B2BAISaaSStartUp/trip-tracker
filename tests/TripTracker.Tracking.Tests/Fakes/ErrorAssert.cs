using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Tracking.Tests.Fakes;

internal static class ErrorAssert
{
    public static void Fails<T>(Result<T, Error> result, ErrorType type, string code)
    {
        Assert.True(result.IsFailure, "Expected a failure but the operation succeeded.");
        Assert.Equal(type, result.Error.Type);
        Assert.Equal(code, result.Error.Code);
    }

    public static void Fails(UnitResult<Error> result, ErrorType type, string code)
    {
        Assert.True(result.IsFailure, "Expected a failure but the operation succeeded.");
        Assert.Equal(type, result.Error.Type);
        Assert.Equal(code, result.Error.Code);
    }
}
