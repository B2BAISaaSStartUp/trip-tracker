using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Web.Models;

namespace TripTracker.Web.Tests.Models;

public class ApiResultTests
{
    [Fact]
    public void ApiResult_Failure_HasNoValue()
    {
        var result = ApiResult<string>.Failure([new ApiError("a.b", "message", ErrorType.FAILURE)]);

        Assert.False(result.IsSuccess);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void ApiResult_FailureWithoutErrors_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => ApiResult<string>.Failure([]));
    }
}
