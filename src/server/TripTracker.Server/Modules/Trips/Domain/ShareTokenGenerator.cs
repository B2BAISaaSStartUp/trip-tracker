using System.Security.Cryptography;

namespace TripTracker.Trips.Domain;

/// <summary>Creates unguessable, URL-safe share tokens.</summary>
internal static class ShareTokenGenerator
{
    private const int ByteCount = 32;

    public static string Generate() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(ByteCount))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
