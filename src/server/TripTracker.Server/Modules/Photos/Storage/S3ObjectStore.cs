using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace TripTracker.Photos.Storage;

/// <summary>Talks to MinIO through the S3 API. Creates the bucket on first write if it does not exist.</summary>
internal sealed class S3ObjectStore : IObjectStore, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private volatile bool _bucketReady;

    public S3ObjectStore(IOptions<PhotoStorageOptions> options)
    {
        var settings = options.Value;
        _bucket = settings.Bucket;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(settings.AccessKey, settings.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = settings.Endpoint,
                ForcePathStyle = settings.ForcePathStyle,
                AuthenticationRegion = settings.Region,
            });
    }

    public async Task PutAsync(string key, byte[] data, string contentType, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);

        using var stream = new MemoryStream(data, writable: false);
        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = stream,
                ContentType = contentType,
            },
            ct);
    }

    public async Task<Stream> GetAsync(string key, CancellationToken ct)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, key, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new ObjectNotFoundException(key);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        await _client.DeleteObjectAsync(_bucket, key, ct);
    }

    public void Dispose()
    {
        _client.Dispose();
        _bucketLock.Dispose();
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady)
        {
            return;
        }

        await _bucketLock.WaitAsync(ct);
        try
        {
            if (_bucketReady)
            {
                return;
            }

            try
            {
                await _client.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, ct);
            }
            catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                // Created earlier, by a previous run or by another instance.
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}
