using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace TripTracker.Web.Tests.Fakes;

/// <summary>Unsigned tokens in the JWT layout. The client never checks signatures.</summary>
internal static class TestTokens
{
    public static string Create(DateTimeOffset expiresAt, string name = "Ann", string role = "User") =>
        WithPayload(new Dictionary<string, object>
        {
            ["sub"] = "6f0c1e4a-3b7d-4c52-9a1e-8d2f5b7c9e10",
            ["email"] = "ann@example.com",
            ["name"] = name,
            ["role"] = role,
            ["exp"] = expiresAt.ToUnixTimeSeconds(),
        });

    public static string WithPayload(object payload) => WithRawPayload(JsonSerializer.Serialize(payload));

    public static string WithRawPayload(string json) =>
        $"{Encode("""{"alg":"HS256","typ":"JWT"}""")}.{Encode(json)}.signature";

    private static string Encode(string text) => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(text));
}
