namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Request to create a trip (FR-06).</summary>
public sealed record CreateTripRequest(
    string Title,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate);
