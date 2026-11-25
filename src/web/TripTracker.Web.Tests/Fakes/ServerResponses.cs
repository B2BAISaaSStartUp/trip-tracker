using System.Text.Json;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Web.Tests.Fakes;

/// <summary>
/// Response bodies as the API writes them: SharedKernel's own envelope with the web serializer defaults,
/// which is what <c>WriteAsJsonAsync</c> uses.
/// </summary>
internal static class ServerResponses
{
    public static string Ok(object result) => JsonSerializer.Serialize(Envelope.Ok(result), JsonSerializerOptions.Web);

    public static string Failed(params Error[] errors) =>
        JsonSerializer.Serialize(Envelope.Error(new ErrorList(errors)), JsonSerializerOptions.Web);

    public static AuthResponse Auth(string accessToken = "access-token") => new(
        accessToken,
        new DateTimeOffset(2026, 11, 25, 13, 0, 0, TimeSpan.Zero),
        "refresh-token",
        new DateTimeOffset(2026, 12, 25, 12, 0, 0, TimeSpan.Zero),
        new UserDto(
            Guid.Parse("6f0c1e4a-3b7d-4c52-9a1e-8d2f5b7c9e10"),
            "ann@example.com",
            "Ann",
            "User",
            false,
            new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero)));
}
