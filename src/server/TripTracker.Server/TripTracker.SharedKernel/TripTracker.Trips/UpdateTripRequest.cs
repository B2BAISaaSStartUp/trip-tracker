namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Request to change a trip's title, dates and description (FR-07).</summary>
public sealed record UpdateTripRequest(
    string Title,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate);
