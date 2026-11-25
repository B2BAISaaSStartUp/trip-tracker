namespace TripTracker.Photos.Storage;

/// <summary>Settings of the MinIO server, read from <c>Photos:Storage</c>.</summary>
internal sealed class PhotoStorageOptions
{
    /// <summary>Address of the S3 API, for example <c>http://localhost:9000</c> for the local Docker setup.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Access key. Keep it in user secrets or an environment variable.</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Secret key. Keep it in user secrets or an environment variable.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "trip-tracker-photos";

    /// <summary>MinIO ignores the region but the signing code needs one.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>MinIO expects <c>host/bucket/key</c> addresses, not <c>bucket.host/key</c>.</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <exception cref="InvalidOperationException">A required setting is missing or invalid.</exception>
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("'Photos:Storage:Endpoint' must be an absolute http or https URL.");
        }

        if (string.IsNullOrWhiteSpace(AccessKey) || string.IsNullOrWhiteSpace(SecretKey))
        {
            throw new InvalidOperationException("'Photos:Storage:AccessKey' and 'Photos:Storage:SecretKey' must be set.");
        }

        if (string.IsNullOrWhiteSpace(Bucket))
        {
            throw new InvalidOperationException("'Photos:Storage:Bucket' must be set.");
        }
    }
}
