namespace TripTracker.SharedKernel.TripTracker.Photos;

/// <summary>Request to attach a photo to a marker, or to detach it when <c>MarkerId</c> is <c>null</c>.</summary>
public sealed record AssignMarkerRequest(Guid? MarkerId);
