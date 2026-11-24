namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Read-only trip view for guests (FR-20). Contains no owner or token data.</summary>
public sealed record SharedTripDto(
    Guid Id,
    string Title,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    TripAccessLevel AccessLevel);
