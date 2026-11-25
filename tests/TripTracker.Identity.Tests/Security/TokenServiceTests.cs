using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Security;
using TripTracker.Identity.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Framework;

namespace TripTracker.Identity.Tests.Security;

public class TokenServiceTests
{
    private readonly JwtOptions _options = new() { SigningKey = new string('k', 48) };
    private readonly FakeClock _clock = new(DateTimeOffset.UtcNow);
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        _service = new TokenService(Options.Create(_options), _clock);
    }

    private TokenValidationParameters ValidationParameters(string key) =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            NameClaimType = "name",
            RoleClaimType = "role",
        };

    [Fact]
    public async Task AccessToken_CarriesTheUserClaims_AndValidates()
    {
        var user = AppUser.Create("a@example.com", "Ann", RoleNames.ADMIN, _clock.GetUtcNow());

        var token = _service.CreateAccessToken(user);
        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, ValidationParameters(_options.SigningKey));

        Assert.True(validation.IsValid, validation.Exception?.Message);
        Assert.Equal(user.Id.ToString(), validation.ClaimsIdentity.FindFirst("sub")?.Value);
        Assert.Equal("a@example.com", validation.ClaimsIdentity.FindFirst("email")?.Value);
        Assert.Equal("Ann", validation.ClaimsIdentity.FindFirst("name")?.Value);
        Assert.Equal(RoleNames.ADMIN, validation.ClaimsIdentity.FindFirst("role")?.Value);
        Assert.True(validation.ClaimsIdentity.IsAuthenticated);
    }

    [Fact]
    public void AccessToken_ExpiresAfterTheConfiguredLifetime()
    {
        var user = AppUser.Create("a@example.com", "Ann", RoleNames.USER, _clock.GetUtcNow());

        var token = _service.CreateAccessToken(user);

        Assert.Equal(_clock.GetUtcNow().AddHours(1), token.ExpiresAt);
    }

    [Fact]
    public async Task AccessToken_SignedWithAnotherKey_IsRejected()
    {
        var user = AppUser.Create("a@example.com", "Ann", RoleNames.USER, _clock.GetUtcNow());
        var token = _service.CreateAccessToken(user);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, ValidationParameters(new string('x', 48)));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void AccessTokens_AreUnique()
    {
        var user = AppUser.Create("a@example.com", "Ann", RoleNames.USER, _clock.GetUtcNow());

        Assert.NotEqual(_service.CreateAccessToken(user).Value, _service.CreateAccessToken(user).Value);
    }

    [Fact]
    public void RefreshToken_IsRandom_HashedForStorage_AndExpiresAfterTheConfiguredLifetime()
    {
        var first = _service.CreateRefreshToken();
        var second = _service.CreateRefreshToken();

        Assert.NotEqual(first.Raw, second.Raw);
        Assert.NotEqual(first.Raw, first.Hash);
        Assert.Equal(first.Hash, _service.HashRefreshToken(first.Raw));
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(_clock.GetUtcNow().AddDays(30), first.ExpiresAt);
        Assert.DoesNotContain('+', first.Raw);
        Assert.DoesNotContain('/', first.Raw);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    public void JwtOptions_WithAShortKey_AreInvalid(string key)
    {
        var options = new JwtOptions { SigningKey = key };

        var error = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("SigningKey", error.Message);
    }

    [Fact]
    public void JwtOptions_WithAGoodKey_AreValid()
    {
        new JwtOptions { SigningKey = new string('k', JwtOptions.MinSigningKeyBytes) }.Validate();
    }
}
