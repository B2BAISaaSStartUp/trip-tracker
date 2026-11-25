using System.Net;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.SharedKernel.TripTracker.Trips;
using TripTracker.Web.Models;
using TripTracker.Web.Services;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Services;

public class ApiClientTests
{
    private static readonly LoginRequest Credentials = new("ann@example.com", "secret-1");

    [Fact]
    public async Task Login_Success_ReturnsValueAndSendsCamelCaseJson()
    {
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.OK, ServerResponses.Ok(ServerResponses.Auth()));

        var result = await Client(http).LoginAsync(Credentials);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Equal(ServerResponses.Auth(), result.Value);
        Assert.Equal(HttpMethod.Post, http.Method);
        Assert.Equal("http://api.test/api/auth/login", http.RequestUri!.AbsoluteUri);
        Assert.Equal("""{"email":"ann@example.com","password":"secret-1"}""", http.RequestBody);
    }

    [Fact]
    public async Task Register_Created_ReturnsValueFromRegisterRoute()
    {
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.Created, ServerResponses.Ok(ServerResponses.Auth()));

        var result = await Client(http).RegisterAsync(new RegisterRequest("ann@example.com", "secret-1", "Ann"));

        Assert.True(result.IsSuccess);
        Assert.Equal("http://api.test/api/auth/register", http.RequestUri!.AbsoluteUri);
        Assert.Equal("""{"email":"ann@example.com","password":"secret-1","displayName":"Ann"}""", http.RequestBody);
    }

    [Fact]
    public async Task GetTrips_Success_ReadsPageWithDatesAndAccessLevel()
    {
        var trip = TestTrips.Trip("Lviv", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 9));
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.OK, ServerResponses.Ok(TestTrips.Page(21, trip)));

        var result = await Client(http).GetTripsAsync(new ListTripsQuery());

        Assert.Equal(HttpMethod.Get, http.Method);
        Assert.Equal("http://api.test/api/trips?newestFirst=true&page=1&pageSize=20", http.RequestUri!.AbsoluteUri);
        Assert.Equal(21, result.Value.TotalCount);
        Assert.Equal(trip, Assert.Single(result.Value.Items));
    }

    [Fact]
    public async Task GetTrips_WithSearchAndOldestFirst_EscapesTheQuery()
    {
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.OK, ServerResponses.Ok(TestTrips.Page(0)));
        var query = new ListTripsQuery { Search = "a&b #1", NewestFirst = false, Page = 2, PageSize = 50 };

        await Client(http).GetTripsAsync(query);

        Assert.Equal("?newestFirst=false&page=2&pageSize=50&search=a%26b%20%231", http.RequestUri!.Query);
    }

    [Fact]
    public async Task Register_BadRequestWithFieldErrors_ReturnsEveryErrorWithItsField()
    {
        var body = ServerResponses.Failed(
            Error.Validation("value.is.required", "Email is required", "Email"),
            Error.Validation("value.too.long", "DisplayName must not exceed 100 characters", "DisplayName"));
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.BadRequest, body);

        var result = await Client(http).RegisterAsync(new RegisterRequest("", "secret-1", "Ann"));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            [
                new ApiError("value.is.required", "Email is required", ErrorType.VALIDATION, "Email"),
                new ApiError(
                    "value.too.long", "DisplayName must not exceed 100 characters", ErrorType.VALIDATION, "DisplayName"),
            ],
            result.Errors);
    }

    [Fact]
    public async Task Login_UnauthorizedWithEnvelope_KeepsTheApiMessage()
    {
        var body = ServerResponses.Failed(Error.Unauthorized("auth.credentials.invalid", "E-mail or password is incorrect"));
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.Unauthorized, body);

        var result = await Client(http).LoginAsync(Credentials);

        var error = Assert.Single(result.Errors);
        Assert.Equal("auth.credentials.invalid", error.Code);
        Assert.Equal("E-mail or password is incorrect", error.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "client.request.invalid", ErrorType.VALIDATION)]
    [InlineData(HttpStatusCode.Unauthorized, "client.auth.required", ErrorType.UNAUTHORIZED)]
    [InlineData(HttpStatusCode.Forbidden, "client.access.forbidden", ErrorType.FORBIDDEN)]
    [InlineData(HttpStatusCode.NotFound, "client.resource.not.found", ErrorType.NOT_FOUND)]
    [InlineData(HttpStatusCode.Conflict, "client.request.conflict", ErrorType.CONFLICT)]
    [InlineData(HttpStatusCode.TooManyRequests, "client.requests.too.many", ErrorType.TOO_MANY_REQUESTS)]
    [InlineData(HttpStatusCode.InternalServerError, "client.server.error", ErrorType.INTERNAL)]
    [InlineData(HttpStatusCode.BadGateway, "client.server.error", ErrorType.INTERNAL)]
    [InlineData(HttpStatusCode.OK, "client.response.invalid", ErrorType.FAILURE)]
    public async Task GetTrips_EmptyBody_ReturnsErrorForTheStatus(HttpStatusCode status, string code, ErrorType type)
    {
        var http = StubHttpMessageHandler.Responding(status);

        var result = await Client(http).GetTripsAsync(new ListTripsQuery());

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(code, error.Code);
        Assert.Equal(type, error.Type);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, "not json", "client.response.invalid")]
    [InlineData(HttpStatusCode.OK, """{"result":null,"errors":null,"isError":false}""", "client.response.invalid")]
    [InlineData(HttpStatusCode.OK, """{"result":"wrong shape"}""", "client.response.invalid")]
    [InlineData(HttpStatusCode.NotFound, "<html><body>Not found</body></html>", "client.resource.not.found")]
    [InlineData(HttpStatusCode.BadRequest, """{"title":"Bad request","errors":{"Email":["required"]}}""", "client.request.invalid")]
    public async Task GetTrips_BodyThatIsNotAUsableEnvelope_ReturnsErrorForTheStatus(
        HttpStatusCode status,
        string body,
        string code)
    {
        var http = StubHttpMessageHandler.Responding(status, body);

        var result = await Client(http).GetTripsAsync(new ListTripsQuery());

        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Login_ServerInternalError_ReplacesTheExceptionText()
    {
        const string exceptionText = "42P01: relation \"identity.users\" does not exist";
        var body = ServerResponses.Failed(Error.Failure("server.internal", exceptionText));
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.InternalServerError, body);

        var result = await Client(http).LoginAsync(Credentials);

        var error = Assert.Single(result.Errors);
        Assert.Equal("server.internal", error.Code);
        Assert.Equal("Something went wrong on the server. Try again later.", error.Message);
        Assert.DoesNotContain("relation", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_ServerUnreachable_ReturnsNetworkError()
    {
        var http = StubHttpMessageHandler.Throwing(new HttpRequestException("connection refused"));

        var result = await Client(http).LoginAsync(Credentials);

        var error = Assert.Single(result.Errors);
        Assert.Equal("client.network.unreachable", error.Code);
        Assert.Equal(ErrorType.FAILURE, error.Type);
    }

    [Fact]
    public async Task Login_HttpClientTimeout_ReturnsTimeoutError()
    {
        var http = StubHttpMessageHandler.Throwing(new TaskCanceledException("timed out", new TimeoutException()));

        var result = await Client(http).LoginAsync(Credentials);

        Assert.Equal("client.network.timeout", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Login_CancelledByTheCaller_Throws()
    {
        var http = StubHttpMessageHandler.Responding(HttpStatusCode.OK);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Client(http).LoginAsync(Credentials, cancellation.Token));
    }

    private static ApiClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") });
}
