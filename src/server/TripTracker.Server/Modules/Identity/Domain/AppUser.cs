using Microsoft.AspNetCore.Identity;
using TripTracker.SharedKernel.TripTracker.Framework;

namespace TripTracker.Identity.Domain;

/// <summary>An account. The role is stored as a plain string (<see cref="RoleNames"/>), so no role tables are needed.</summary>
internal sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>Longest display name accepted.</summary>
    public const int DisplayNameMaxLength = 100;

    /// <summary>Longest role string stored.</summary>
    public const int RoleMaxLength = 16;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>One of <see cref="RoleNames.USER"/> and <see cref="RoleNames.ADMIN"/>.</summary>
    public string Role { get; private set; } = RoleNames.USER;

    /// <summary>Set by an administrator; a blocked account cannot sign in.</summary>
    public bool IsBlocked { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates an account whose user name is its e-mail.</summary>
    public static AppUser Create(
        string email,
        string displayName,
        string role,
        DateTimeOffset now,
        bool emailConfirmed = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
            DisplayName = displayName,
            Role = role,
            CreatedAt = now,
        };

    public void Block() => IsBlocked = true;

    public void Unblock() => IsBlocked = false;
}
