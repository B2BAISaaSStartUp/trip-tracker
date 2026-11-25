using TripTracker.Photos.Storage;

namespace TripTracker.Photos.Tests.Fakes;

/// <summary>A dictionary standing in for MinIO. Can be told to fail, to simulate an outage.</summary>
internal sealed class InMemoryObjectStore : IObjectStore
{
    private readonly Dictionary<string, (byte[] Data, string ContentType)> _objects = new();

    public bool FailOnDelete { get; set; }

    public IReadOnlyCollection<string> Keys => _objects.Keys;

    public byte[] Read(string key) => _objects[key].Data;

    public string ContentTypeOf(string key) => _objects[key].ContentType;

    public bool Contains(string key) => _objects.ContainsKey(key);

    public void Remove(string key) => _objects.Remove(key);

    public Task PutAsync(string key, byte[] data, string contentType, CancellationToken ct)
    {
        _objects[key] = (data, contentType);
        return Task.CompletedTask;
    }

    public Task<Stream> GetAsync(string key, CancellationToken ct)
    {
        if (!_objects.TryGetValue(key, out var stored))
        {
            throw new ObjectNotFoundException(key);
        }

        return Task.FromResult<Stream>(new MemoryStream(stored.Data, writable: false));
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        if (FailOnDelete)
        {
            throw new IOException("The object store is down.");
        }

        _objects.Remove(key);
        return Task.CompletedTask;
    }
}
