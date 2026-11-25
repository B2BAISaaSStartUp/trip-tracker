using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using TripTracker.Identity.Contracts;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Persistence;
using TripTracker.Identity.Security;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Core.Validation;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Application;

internal sealed class AuthService : IAuthService
{
    private const string GoogleProvider = "Google";

    private readonly UserManager<AppUser> _users;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly IGoogleTokenValidator _google;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public AuthService(
        UserManager<AppUser> users,
        IRefreshTokenStore refreshTokens,
        ITokenService tokens,
        IGoogleTokenValidator google,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _google = google;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<AuthResponse, Error>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var validation = new RegisterRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return validation.ToList().First();
        }

        var email = request.Email.Trim();
        if (await _users.FindByEmailAsync(email) is not null)
        {
            return IdentityErrors.EmailTaken();
        }

        var user = AppUser.Create(email, request.DisplayName.Trim(), RoleNames.USER, _clock.GetUtcNow());
        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return IdentityErrors.FromIdentityResult(created);
        }

        return await IssueAsync(user, ct);
    }

    public async Task<Result<AuthResponse, Error>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return IdentityErrors.InvalidCredentials();
        }

        var user = await _users.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return IdentityErrors.InvalidCredentials();
        }

        if (await _users.IsLockedOutAsync(user))
        {
            return IdentityErrors.LockedOut();
        }

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            return await _users.IsLockedOutAsync(user)
                ? IdentityErrors.LockedOut()
                : IdentityErrors.InvalidCredentials();
        }

        await _users.ResetAccessFailedCountAsync(user);

        // Checked after the password, so only someone who knows it learns that the account is blocked.
        if (user.IsBlocked)
        {
            return IdentityErrors.AccountBlocked();
        }

        return await IssueAsync(user, ct);
    }

    public async Task<Result<AuthResponse, Error>> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return IdentityErrors.GoogleTokenInvalid();
        }

        var validated = await _google.ValidateAsync(request.IdToken, ct);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        var google = validated.Value;
        if (!google.EmailVerified || string.IsNullOrWhiteSpace(google.Email))
        {
            return IdentityErrors.GoogleEmailNotVerified();
        }

        var user = await _users.FindByLoginAsync(GoogleProvider, google.Subject);
        if (user is null)
        {
            // Google vouches for the e-mail, so an account created with a password can be linked to it.
            user = await _users.FindByEmailAsync(google.Email);
            if (user is null)
            {
                var name = string.IsNullOrWhiteSpace(google.Name) ? google.Email : google.Name.Trim();
                user = AppUser.Create(
                    google.Email,
                    Truncate(name, AppUser.DisplayNameMaxLength),
                    RoleNames.USER,
                    _clock.GetUtcNow(),
                    emailConfirmed: true);

                var created = await _users.CreateAsync(user);
                if (!created.Succeeded)
                {
                    return IdentityErrors.FromIdentityResult(created);
                }
            }

            var linked = await _users.AddLoginAsync(user, new UserLoginInfo(GoogleProvider, google.Subject, GoogleProvider));
            if (!linked.Succeeded)
            {
                return IdentityErrors.FromIdentityResult(linked);
            }
        }

        if (user.IsBlocked)
        {
            return IdentityErrors.AccountBlocked();
        }

        return await IssueAsync(user, ct);
    }

    public async Task<Result<AuthResponse, Error>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return IdentityErrors.RefreshTokenInvalid();
        }

        var now = _clock.GetUtcNow();
        var stored = await _refreshTokens.FindByHashAsync(_tokens.HashRefreshToken(request.RefreshToken), ct);
        if (stored is null)
        {
            return IdentityErrors.RefreshTokenInvalid();
        }

        if (stored.IsRevoked)
        {
            // A token that was already exchanged is being used again: assume it leaked and end every session.
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, now, ct);
            return IdentityErrors.RefreshTokenInvalid();
        }

        if (stored.IsExpired(now))
        {
            return IdentityErrors.RefreshTokenInvalid();
        }

        var user = await _users.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
        {
            return IdentityErrors.RefreshTokenInvalid();
        }

        if (user.IsBlocked)
        {
            await _refreshTokens.RevokeAllForUserAsync(user.Id, now, ct);
            return IdentityErrors.AccountBlocked();
        }

        var next = _tokens.CreateRefreshToken();
        var nextEntity = RefreshToken.Create(user.Id, next.Hash, now, next.ExpiresAt);
        stored.Revoke(now, nextEntity.Id);

        // Saves the revoked token and the new one together.
        await _refreshTokens.AddAsync(nextEntity, ct);

        return Build(user, _tokens.CreateAccessToken(user), next);
    }

    public async Task<UnitResult<Error>> LogoutAsync(RefreshRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var stored = await _refreshTokens.FindByHashAsync(_tokens.HashRefreshToken(request.RefreshToken), ct);
            if (stored is not null && !stored.IsRevoked)
            {
                stored.Revoke(_clock.GetUtcNow());
                await _refreshTokens.SaveAsync(ct);
            }
        }

        return UnitResult.Success<Error>();
    }

    public async Task<Result<UserDto, Error>> GetCurrentUserAsync(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return IdentityErrors.NotAuthenticated();
        }

        var user = await _users.FindByIdAsync(_currentUser.UserId.ToString());
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        return user.ToDto();
    }

    private async Task<Result<AuthResponse, Error>> IssueAsync(AppUser user, CancellationToken ct)
    {
        var access = _tokens.CreateAccessToken(user);
        var refresh = _tokens.CreateRefreshToken();

        await _refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, refresh.Hash, _clock.GetUtcNow(), refresh.ExpiresAt),
            ct);

        return Build(user, access, refresh);
    }

    private static AuthResponse Build(AppUser user, AccessToken access, NewRefreshToken refresh) =>
        new(access.Value, access.ExpiresAt, refresh.Raw, refresh.ExpiresAt, user.ToDto());

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
