namespace TripTracker.Photos.Storage;

/// <summary>Port to the binary object store (MinIO over its S3 API). Nothing outside this module may reach it.</summary>
internal interface IObjectStore
{
    /// <summary>Writes an object, replacing any object with the same key.</summary>
    Task PutAsync(string key, byte[] data, string contentType, CancellationToken ct);

    /// <summary>Opens an object for reading. The caller disposes the stream.</summary>
    /// <exception cref="ObjectNotFoundException">There is no object with this key.</exception>
    Task<Stream> GetAsync(string key, CancellationToken ct);

    /// <summary>Deletes an object. Deleting a missing object is not an error.</summary>
    Task DeleteAsync(string key, CancellationToken ct);
}

internal sealed class ObjectNotFoundException : Exception
{
    public ObjectNotFoundException(string key)
        : base($"Object '{key}' was not found.")
    {
    }
}
