using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using TripTracker.Identity.Contracts;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Persistence;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Application;

internal sealed class UserManagementService : IUserManagement
{
    private readonly UserManager<AppUser> _users;
    private readonly IRefreshTokenStore _refreshTokens;
    private readonly TimeProvider _clock;

    public UserManagementService(UserManager<AppUser> users, IRefreshTokenStore refreshTokens, TimeProvider clock)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<Result<UserDto, Error>> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        return user.ToDto();
    }

    public async Task<UnitResult<Error>> BlockAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        if (user.Role == RoleNames.ADMIN)
        {
            return IdentityErrors.AdminCannotBeBlocked();
        }

        if (!user.IsBlocked)
        {
            user.Block();
            var updated = await _users.UpdateAsync(user);
            if (!updated.Succeeded)
            {
                return IdentityErrors.FromIdentityResult(updated);
            }
        }

        await _refreshTokens.RevokeAllForUserAsync(userId, _clock.GetUtcNow(), ct);
        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> UnblockAsync(Guid userId, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        if (user.IsBlocked)
        {
            user.Unblock();
            var updated = await _users.UpdateAsync(user);
            if (!updated.Succeeded)
            {
                return IdentityErrors.FromIdentityResult(updated);
            }
        }

        return UnitResult.Success<Error>();
    }
}
