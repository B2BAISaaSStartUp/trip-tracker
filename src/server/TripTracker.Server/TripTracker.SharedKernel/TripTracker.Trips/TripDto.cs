namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Trip representation returned to authenticated callers. <c>ShareToken</c> is set only for the owner.</summary>
public sealed record TripDto(
    Guid Id,
    Guid OwnerId,
    string Title,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    TripAccessLevel AccessLevel,
    string? ShareToken,
    bool IsHidden,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
