using CSharpFunctionalExtensions;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Contracts;

/// <summary>
/// Account management for the Administration module (FR-21). The caller is responsible for checking the Admin role.
/// A blocked user cannot sign in or refresh a token; an access token that was already issued works until it expires.
/// </summary>
public interface IUserManagement
{
    /// <summary>Gets an account by id. Fails with NotFound when it does not exist.</summary>
    Task<Result<UserDto, Error>> GetByIdAsync(Guid userId, CancellationToken ct);

    /// <summary>Blocks an account and ends its sessions. Fails with Conflict for an Admin account. Blocking a blocked account succeeds.</summary>
    Task<UnitResult<Error>> BlockAsync(Guid userId, CancellationToken ct);

    /// <summary>Lets a blocked account sign in again. Unblocking an active account succeeds.</summary>
    Task<UnitResult<Error>> UnblockAsync(Guid userId, CancellationToken ct);
}
