namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Full trip representation returned to the owner.</summary>
public sealed record TripDto(
    Guid Id,
    Guid OwnerId,
    string Title,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    TripAccessLevel AccessLevel,
    string ShareToken,
    bool IsHidden,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
