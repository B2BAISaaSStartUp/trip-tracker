using System.Buffers.Text;
using System.Security.Claims;
using System.Text.Json;

namespace TripTracker.Web.Services;

/// <summary>What the client needs from an access token.</summary>
public sealed record JwtContent(IReadOnlyList<Claim> Claims, DateTimeOffset ExpiresAt);

/// <summary>
/// Reads the payload of a JWT. The signature is not checked: the client has no key, and the API checks
/// every token it receives. The result only decides what the UI shows.
/// </summary>
public static class JwtReader
{
    /// <summary>Returns <c>null</c> when the text is not a JWT with a JSON payload and an expiry time.</summary>
    public static JwtContent? Read(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            using var payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
            if (payload.RootElement.ValueKind != JsonValueKind.Object
                || !payload.RootElement.TryGetProperty("exp", out var exp)
                || exp.ValueKind != JsonValueKind.Number
                || !exp.TryGetInt64(out var expiresAt))
            {
                return null;
            }

            var claims = new List<Claim>();
            foreach (var property in payload.RootElement.EnumerateObject())
            {
                // A claim with several values, such as several roles, arrives as an array.
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    claims.AddRange(property.Value.EnumerateArray().Select(item => ToClaim(property.Name, item)));
                }
                else
                {
                    claims.Add(ToClaim(property.Name, property.Value));
                }
            }

            return new JwtContent(claims, DateTimeOffset.FromUnixTimeSeconds(expiresAt));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static Claim ToClaim(string type, JsonElement value) =>
        new(type, value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText());
}
