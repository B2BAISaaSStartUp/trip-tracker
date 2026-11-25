namespace TripTracker.Photos.Contracts;

/// <summary>A photo file ready to be sent to the client.</summary>
/// <param name="Content">The bytes. Whoever receives this disposes it, normally by sending it as the response.</param>
/// <param name="ContentType">MIME type of <paramref name="Content"/>.</param>
public sealed record PhotoContent(Stream Content, string ContentType);
