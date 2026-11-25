using System.Security.Claims;
using TripTracker.Web.Services;
using TripTracker.Web.Tests.Fakes;

namespace TripTracker.Web.Tests.Services;

public class JwtReaderTests
{
    private static readonly DateTimeOffset Expiry = new(2026, 11, 25, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Read_ValidToken_ReturnsClaimsAndExpiry()
    {
        var content = JwtReader.Read(TestTokens.Create(Expiry, name: "Ann", role: "Admin"));

        Assert.NotNull(content);
        Assert.Equal(Expiry, content.ExpiresAt);
        Assert.Equal("Ann", Value(content, "name"));
        Assert.Equal("Admin", Value(content, "role"));
        Assert.Equal("ann@example.com", Value(content, "email"));
        Assert.Equal("6f0c1e4a-3b7d-4c52-9a1e-8d2f5b7c9e10", Value(content, "sub"));
    }

    [Theory]
    [InlineData("Дмитро Ковальчук")]
    [InlineData("??>>~~")]
    public void Read_NameNeedingUrlSafeBase64_IsDecoded(string name)
    {
        var content = JwtReader.Read(TestTokens.Create(Expiry, name));

        Assert.Equal(name, Value(content!, "name"));
    }

    [Fact]
    public void Read_ArrayClaim_BecomesOneClaimPerValue()
    {
        var token = TestTokens.WithPayload(new { role = new[] { "User", "Admin" }, exp = Expiry.ToUnixTimeSeconds() });

        var content = JwtReader.Read(token);

        Assert.Equal(["User", "Admin"], content!.Claims.Where(c => c.Type == "role").Select(c => c.Value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("only.two")]
    [InlineData("one.two.three.four")]
    [InlineData("header.!!!not-base64!!!.signature")]
    [InlineData("header.bm90IGpzb24.signature")]
    public void Read_TextThatIsNotAJwt_ReturnsNull(string token)
    {
        Assert.Null(JwtReader.Read(token));
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("""{"name":"Ann"}""")]
    [InlineData("""{"exp":"1795611600"}""")]
    [InlineData("""{"exp":1795611600.5}""")]
    [InlineData("""{"exp":999999999999999}""")]
    public void Read_PayloadWithoutAUsableExpiry_ReturnsNull(string payload)
    {
        Assert.Null(JwtReader.Read(TestTokens.WithRawPayload(payload)));
    }

    private static string Value(JwtContent content, string type) =>
        content.Claims.Single((Claim c) => c.Type == type).Value;
}
