namespace TripTracker.Photos.Contracts;

/// <summary>A file to store. The format is detected from the bytes; any content type sent by the client is ignored.</summary>
/// <param name="Content">The file. The caller disposes it.</param>
/// <param name="FileName">Name shown in the gallery; path parts are dropped.</param>
/// <param name="Length">Declared size in bytes, checked before the file is read.</param>
/// <param name="MarkerId">Marker to attach the photo to, or <c>null</c> for the trip gallery only.</param>
public sealed record PhotoUploadRequest(Stream Content, string FileName, long Length, Guid? MarkerId);
