namespace TripTracker.SharedKernel.TripTracker.Identity;

/// <summary>Public view of an account. Never contains password data.</summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsBlocked,
    DateTimeOffset CreatedAt);
