namespace TripTracker.SharedKernel.TripTracker.Tracking;

/// <summary>A batch of route points sent from the app's offline buffer.</summary>
/// <param name="Points">1 to <see cref="MaxPoints"/> points, in any order.</param>
public sealed record UploadTrackPointsRequest(IReadOnlyList<TrackPointRequest> Points)
{
    /// <summary>Largest batch accepted; the app splits a bigger buffer into several requests.</summary>
    public const int MaxPoints = 500;
}
