namespace TripTracker.SharedKernel.TripTracker.Trips;

/// <summary>Request to change a trip's access level (FR-19).</summary>
public sealed record ChangeAccessLevelRequest(TripAccessLevel AccessLevel);
