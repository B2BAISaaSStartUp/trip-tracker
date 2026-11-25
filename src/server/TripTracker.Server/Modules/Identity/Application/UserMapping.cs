using TripTracker.Identity.Domain;
using TripTracker.SharedKernel.TripTracker.Identity;

namespace TripTracker.Identity.Application;

internal static class UserMapping
{
    public static UserDto ToDto(this AppUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.Role, user.IsBlocked, user.CreatedAt);
}
