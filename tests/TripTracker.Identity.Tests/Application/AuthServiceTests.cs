using CSharpFunctionalExtensions;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Tests.Application;

public class AuthServiceTests : IDisposable
{
    private readonly IdentityTestBed _bed = new();

    public void Dispose() => _bed.Dispose();

    private Task<Result<AuthResponse, Error>> Login(string email, string password) =>
        _bed.Auth.LoginAsync(new LoginRequest(email, password), CancellationToken.None);

    private Task<Result<AuthResponse, Error>> Refresh(string token) =>
        _bed.Auth.RefreshAsync(new RefreshRequest(token), CancellationToken.None);

    // ---------- Register ----------

    [Fact]
    public async Task Register_CreatesUserAccount_AndIssuesTokens()
    {
        var result = await _bed.RegisterAsync();

        Assert.True(result.IsSuccess);
        var response = result.Value;
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal(IdentityTestBed.Email, response.User.Email);
        Assert.Equal("Traveller", response.User.DisplayName);
        Assert.Equal(RoleNames.USER, response.User.Role);
        Assert.False(response.User.IsBlocked);
        Assert.Equal(_bed.Clock.GetUtcNow().AddHours(1), response.AccessTokenExpiresAt);
        Assert.Equal(_bed.Clock.GetUtcNow().AddDays(30), response.RefreshTokenExpiresAt);
    }

    [Fact]
    public async Task Register_DoesNotStoreThePassword()
    {
        await _bed.RegisterAsync();

        var user = await _bed.Manager.FindByEmailAsync(IdentityTestBed.Email);

        Assert.NotNull(user);
        Assert.NotEqual(IdentityTestBed.Password, user.PasswordHash);
        Assert.True(await _bed.Manager.CheckPasswordAsync(user, IdentityTestBed.Password));
    }

    [Fact]
    public async Task Register_SameEmailTwice_IsRejected_IgnoringCase()
    {
        await _bed.RegisterAsync("a@example.com");

        var second = await _bed.RegisterAsync("A@Example.com");

        ErrorAssert.Fails(second, ErrorType.ALREADY_EXISTS, "auth.email.taken");
    }

    [Theory]
    [InlineData("", "Passw0rd", "Name")]
    [InlineData("not-an-email", "Passw0rd", "Name")]
    [InlineData("a@example.com", "", "Name")]
    [InlineData("a@example.com", "Passw0rd", "")]
    public async Task Register_WithInvalidInput_ReturnsValidationError(string email, string password, string name)
    {
        var result = await _bed.RegisterAsync(email, password, name);

        ErrorAssert.Fails(result, ErrorType.VALIDATION);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("nodigitshere")]
    public async Task Register_WithWeakPassword_ReturnsValidationError(string password)
    {
        var result = await _bed.RegisterAsync(password: password);

        ErrorAssert.Fails(result, ErrorType.VALIDATION, "auth.password.weak");
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithCorrectPassword_Succeeds_IgnoringEmailCase()
    {
        await _bed.RegisterAsync();

        var result = await Login(IdentityTestBed.Email.ToUpperInvariant(), IdentityTestBed.Password);

        Assert.True(result.IsSuccess);
        Assert.Equal(IdentityTestBed.Email, result.Value.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _bed.RegisterAsync();

        var result = await Login(IdentityTestBed.Email, "Wrong-Passw0rd");

        ErrorAssert.Fails(result, ErrorType.UNAUTHORIZED, "auth.credentials.invalid");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_GivesTheSameErrorAsAWrongPassword()
    {
        var result = await Login("nobody@example.com", IdentityTestBed.Password);

        ErrorAssert.Fails(result, ErrorType.UNAUTHORIZED, "auth.credentials.invalid");
    }

    [Theory]
    [InlineData("", "x")]
    [InlineData("a@example.com", "")]
    public async Task Login_WithEmptyFields_ReturnsUnauthorized(string email, string password)
    {
        var result = await Login(email, password);

        ErrorAssert.Fails(result, ErrorType.UNAUTHORIZED);
    }

    [Fact]
    public async Task Login_LocksTheAccount_AfterFiveWrongPasswords()
    {
        await _bed.RegisterAsync();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            ErrorAssert.Fails(await Login(IdentityTestBed.Email, "Wrong-Passw0rd"), ErrorType.UNAUTHORIZED);
        }

        ErrorAssert.Fails(await Login(IdentityTestBed.Email, "Wrong-Passw0rd"), ErrorType.TOO_MANY_REQUESTS, "auth.account.locked");

        // Even the right password is refused while the account is locked.
        ErrorAssert.Fails(await Login(IdentityTestBed.Email, IdentityTestBed.Password), ErrorType.TOO_MANY_REQUESTS);
    }

    [Fact]
    public async Task Login_ResetsTheFailureCount_AfterASuccess()
    {
        await _bed.RegisterAsync();

        for (var round = 0; round < 3; round++)
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                await Login(IdentityTestBed.Email, "Wrong-Passw0rd");
            }

            Assert.True((await Login(IdentityTestBed.Email, IdentityTestBed.Password)).IsSuccess);
        }
    }

    [Fact]
    public async Task Login_ByBlockedUser_IsForbidden_AndUnblockingRestoresIt()
    {
        var registered = await _bed.RegisterAsync();
        var userId = registered.Value.User.Id;

        Assert.True((await _bed.Users.BlockAsync(userId, CancellationToken.None)).IsSuccess);
        ErrorAssert.Fails(await Login(IdentityTestBed.Email, IdentityTestBed.Password), ErrorType.FORBIDDEN, "auth.account.blocked");

        Assert.True((await _bed.Users.UnblockAsync(userId, CancellationToken.None)).IsSuccess);
        Assert.True((await Login(IdentityTestBed.Email, IdentityTestBed.Password)).IsSuccess);
    }

    // ---------- Refresh and logout ----------

    [Fact]
    public async Task Refresh_IssuesNewTokens_AndTheOldRefreshTokenStopsWorking()
    {
        var first = (await _bed.RegisterAsync()).Value;

        var second = await Refresh(first.RefreshToken);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.RefreshToken, second.Value.RefreshToken);
        Assert.NotEqual(first.AccessToken, second.Value.AccessToken);
        Assert.Equal(first.User.Id, second.Value.User.Id);
        ErrorAssert.Fails(await Refresh(first.RefreshToken), ErrorType.UNAUTHORIZED, "auth.refresh.invalid");
    }

    [Fact]
    public async Task Refresh_WithAnAlreadyUsedToken_EndsEverySessionOfTheUser()
    {
        var first = (await _bed.RegisterAsync()).Value;
        var second = (await Refresh(first.RefreshToken)).Value;

        // Someone replays the first token: the newer one must stop working too.
        ErrorAssert.Fails(await Refresh(first.RefreshToken), ErrorType.UNAUTHORIZED);
        ErrorAssert.Fails(await Refresh(second.RefreshToken), ErrorType.UNAUTHORIZED);
    }

    [Fact]
    public async Task Refresh_AfterItExpired_IsRefused()
    {
        var first = (await _bed.RegisterAsync()).Value;

        _bed.Clock.Advance(TimeSpan.FromDays(31));

        ErrorAssert.Fails(await Refresh(first.RefreshToken), ErrorType.UNAUTHORIZED, "auth.refresh.invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-real-token")]
    public async Task Refresh_WithMissingOrUnknownToken_IsRefused(string token)
    {
        ErrorAssert.Fails(await Refresh(token), ErrorType.UNAUTHORIZED, "auth.refresh.invalid");
    }

    [Fact]
    public async Task Refresh_AfterTheUserWasBlocked_IsRefused()
    {
        var first = (await _bed.RegisterAsync()).Value;
        await _bed.Users.BlockAsync(first.User.Id, CancellationToken.None);

        ErrorAssert.Fails(await Refresh(first.RefreshToken), ErrorType.UNAUTHORIZED);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var first = (await _bed.RegisterAsync()).Value;

        var loggedOut = await _bed.Auth.LogoutAsync(new RefreshRequest(first.RefreshToken), CancellationToken.None);

        Assert.True(loggedOut.IsSuccess);
        ErrorAssert.Fails(await Refresh(first.RefreshToken), ErrorType.UNAUTHORIZED);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task Logout_WithMissingOrUnknownToken_StillSucceeds(string token)
    {
        var result = await _bed.Auth.LogoutAsync(new RefreshRequest(token), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ---------- Google ----------

    private Task<Result<AuthResponse, Error>> Google(string token = FakeGoogleTokenValidator.ValidToken) =>
        _bed.Auth.LoginWithGoogleAsync(new GoogleLoginRequest(token), CancellationToken.None);

    [Fact]
    public async Task GoogleLogin_CreatesTheAccount_OnFirstUse()
    {
        var result = await Google();

        Assert.True(result.IsSuccess);
        Assert.Equal("traveller@example.com", result.Value.User.Email);
        Assert.Equal("Traveller", result.Value.User.DisplayName);
        Assert.Equal(RoleNames.USER, result.Value.User.Role);

        var stored = await _bed.Manager.FindByEmailAsync("traveller@example.com");
        Assert.NotNull(stored);
        Assert.True(stored.EmailConfirmed);
    }

    [Fact]
    public async Task GoogleLogin_ASecondTime_SignsInTheSameAccount()
    {
        var first = (await Google()).Value;

        var second = await Google();

        Assert.True(second.IsSuccess);
        Assert.Equal(first.User.Id, second.Value.User.Id);
    }

    [Fact]
    public async Task GoogleLogin_LinksAnAccountThatWasCreatedWithAPassword()
    {
        var registered = (await _bed.RegisterAsync(IdentityTestBed.Email)).Value;

        var google = await Google();

        Assert.True(google.IsSuccess);
        Assert.Equal(registered.User.Id, google.Value.User.Id);
    }

    [Fact]
    public async Task GoogleLogin_WithAnUnverifiedEmail_IsRefused()
    {
        _bed.Google.Identity = new("google-sub-2", "x@example.com", false, "X");

        ErrorAssert.Fails(await Google(), ErrorType.UNAUTHORIZED, "auth.google.email.not.verified");
    }

    [Theory]
    [InlineData("")]
    [InlineData("forged")]
    public async Task GoogleLogin_WithAnInvalidToken_IsRefused(string token)
    {
        ErrorAssert.Fails(await Google(token), ErrorType.UNAUTHORIZED, "auth.google.invalid");
    }

    [Fact]
    public async Task GoogleLogin_ByBlockedUser_IsForbidden()
    {
        var created = (await Google()).Value;
        await _bed.Users.BlockAsync(created.User.Id, CancellationToken.None);

        ErrorAssert.Fails(await Google(), ErrorType.FORBIDDEN, "auth.account.blocked");
    }

    [Fact]
    public async Task GoogleLogin_LongName_IsShortenedToFit()
    {
        _bed.Google.Identity = new("google-sub-3", "long@example.com", true, new string('N', 300));

        var result = await Google();

        Assert.True(result.IsSuccess);
        Assert.Equal(AppUser.DisplayNameMaxLength, result.Value.User.DisplayName.Length);
    }

    // ---------- Current user ----------

    [Fact]
    public async Task GetCurrentUser_ReturnsTheSignedInAccount()
    {
        var registered = (await _bed.RegisterAsync()).Value;
        _bed.User.SignInAs(registered.User.Id);

        var result = await _bed.Auth.GetCurrentUserAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(registered.User.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotSignedIn_IsUnauthorized()
    {
        var result = await _bed.Auth.GetCurrentUserAsync(CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.UNAUTHORIZED);
    }

    [Fact]
    public async Task GetCurrentUser_ForADeletedAccount_IsNotFound()
    {
        _bed.User.SignInAs(Guid.NewGuid());

        var result = await _bed.Auth.GetCurrentUserAsync(CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND);
    }
}
