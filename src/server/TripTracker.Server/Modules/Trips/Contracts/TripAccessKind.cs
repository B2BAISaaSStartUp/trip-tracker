namespace TripTracker.Trips.Contracts;

/// <summary>What the caller wants to do with a trip.</summary>
public enum TripAccessKind
{
    /// <summary>View the trip and its contents.</summary>
    Read,

    /// <summary>Change or delete the trip and its contents.</summary>
    Write,
}
