namespace TripTracker.Trips.Contracts;

/// <summary>Who is asking for what access to which trip.</summary>
/// <param name="TripId">The trip being accessed.</param>
/// <param name="UserId">The signed-in caller, or <c>null</c> for a guest.</param>
/// <param name="ShareToken">The token from a share link, or <c>null</c> if the caller has none.</param>
/// <param name="Kind">Read or write.</param>
public sealed record TripAccessRequest(
    Guid TripId,
    Guid? UserId,
    string? ShareToken,
    TripAccessKind Kind);
