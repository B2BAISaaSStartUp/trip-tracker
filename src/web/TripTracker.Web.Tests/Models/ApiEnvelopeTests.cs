using System.Text.Json;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;
using TripTracker.Web.Models;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Models;

public class ApiEnvelopeTests
{
    [Fact]
    public void Envelope_HandWrittenSuccess_IsRead()
    {
        const string json = """
            {
              "result": {
                "accessToken": "a.b.c",
                "accessTokenExpiresAt": "2026-11-25T13:00:00+00:00",
                "refreshToken": "r",
                "refreshTokenExpiresAt": "2026-12-25T12:00:00+00:00",
                "user": {
                  "id": "6f0c1e4a-3b7d-4c52-9a1e-8d2f5b7c9e10",
                  "email": "ann@example.com",
                  "displayName": "Ann",
                  "role": "User",
                  "isBlocked": false,
                  "createdAt": "2026-11-01T08:30:00+00:00"
                }
              },
              "errors": null,
              "isError": false,
              "timeGenerated": "2026-11-25T14:00:00.1234567+02:00"
            }
            """;

        var envelope = Read<AuthResponse>(json);

        Assert.False(envelope.IsError);
        Assert.Null(envelope.Errors);
        Assert.Equal("a.b.c", envelope.Result!.AccessToken);
        Assert.Equal("Ann", envelope.Result.User.DisplayName);
        Assert.NotEqual(default, envelope.TimeGenerated);
    }

    [Fact]
    public void Envelope_HandWrittenWithTwoErrors_KeepsCodesMessagesTypesAndFields()
    {
        // The API sends the error type as a number: 0 is VALIDATION, 5 is ALREADY_EXISTS.
        const string json = """
            {
              "result": null,
              "errors": [
                { "code": "value.is.required", "message": "Email is required", "type": 0, "invalidField": "Email" },
                { "code": "auth.email.taken", "message": "An account with this e-mail already exists", "type": 5, "invalidField": null }
              ],
              "isError": true,
              "timeGenerated": "2026-11-25T14:00:00+02:00"
            }
            """;

        var envelope = Read<AuthResponse>(json);

        Assert.True(envelope.IsError);
        Assert.Null(envelope.Result);
        Assert.Equal(
            [
                new ApiError("value.is.required", "Email is required", ErrorType.VALIDATION, "Email"),
                new ApiError("auth.email.taken", "An account with this e-mail already exists", ErrorType.ALREADY_EXISTS),
            ],
            envelope.Errors!);
    }

    /// <summary>One error from each public factory of SharedKernel.</summary>
    public static TheoryData<string> ErrorFactories => new(Enum.GetNames<ErrorType>());

    [Theory]
    [MemberData(nameof(ErrorFactories))]
    public void Envelope_SerializedBySharedKernel_KeepsEveryErrorType(string typeName)
    {
        var type = Enum.Parse<ErrorType>(typeName);
        var error = type switch
        {
            ErrorType.VALIDATION => Error.Validation("a.b", "message", "Field"),
            ErrorType.NOT_FOUND => Error.NotFound("a.b", "message"),
            ErrorType.FAILURE => Error.Failure("a.b", "message"),
            ErrorType.CONFLICT => Error.Conflict("a.b", "message"),
            ErrorType.INTERNAL => Error.Internal("a.b", "message"),
            ErrorType.ALREADY_EXISTS => Error.AlreadyExist("a.b", "message"),
            ErrorType.FORBIDDEN => Error.Forbidden("a.b", "message"),
            ErrorType.UNAUTHORIZED => Error.Unauthorized("a.b", "message"),
            ErrorType.TOO_MANY_REQUESTS => Error.TooManyRequests("a.b", "message"),
            _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "SharedKernel has a new error type."),
        };

        var envelope = Read<AuthResponse>(ServerResponses.Failed(error));

        var read = Assert.Single(envelope.Errors!);
        Assert.Equal(new ApiError(error.Code, error.Message, type, error.InvalidField), read);
    }

    [Fact]
    public void Envelope_SuccessSerializedBySharedKernel_IsRead()
    {
        var sent = ServerResponses.Auth();

        var envelope = Read<AuthResponse>(ServerResponses.Ok(sent));

        Assert.False(envelope.IsError);
        Assert.Equal(sent, envelope.Result);
    }

    private static ApiEnvelope<T> Read<T>(string json) =>
        JsonSerializer.Deserialize<ApiEnvelope<T>>(json, JsonSerializerOptions.Web)!;
}
