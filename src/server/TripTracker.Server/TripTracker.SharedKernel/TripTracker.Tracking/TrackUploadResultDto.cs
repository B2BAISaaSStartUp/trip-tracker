namespace TripTracker.SharedKernel.TripTracker.Tracking;

/// <summary>What happened to an uploaded batch. The app may clear its buffer once it gets this answer.</summary>
/// <param name="Received">Points in the request.</param>
/// <param name="Stored">Points stored by this request.</param>
/// <param name="Duplicates">Points skipped because they were already stored or repeated in the batch.</param>
public sealed record TrackUploadResultDto(int Received, int Stored, int Duplicates);
