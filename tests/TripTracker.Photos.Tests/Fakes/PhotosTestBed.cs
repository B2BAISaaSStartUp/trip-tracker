using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using TripTracker.Photos.Application;
using TripTracker.Photos.Contracts;
using TripTracker.Photos.Domain;
using TripTracker.Photos.Imaging;
using TripTracker.SharedKernel.TripTracker.Photos;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Photos.Tests.Fakes;

/// <summary>The real service with the real image code, over an in-memory repository and object store.</summary>
internal sealed class PhotosTestBed
{
    public static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Stranger = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Trip = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid OtherTrip = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid Marker = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid OtherMarker = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public PhotosTestBed()
    {
        Repository = new InMemoryPhotoRepository();
        Store = new InMemoryObjectStore();
        User = new FakeCurrentUser();
        User.SignInAs(Owner);

        Service = new PhotoService(
            Repository,
            Store,
            new ImageSharpProcessor(),
            User,
            TimeProvider.System,
            NullLogger<PhotoService>.Instance);
    }

    public InMemoryPhotoRepository Repository { get; }

    public InMemoryObjectStore Store { get; }

    public FakeCurrentUser User { get; }

    public PhotoService Service { get; }

    public Task<Result<PhotoDto, Error>> UploadAsync(
        byte[] data,
        Guid? markerId = null,
        string fileName = "photo.jpg",
        Guid? tripId = null)
    {
        var stream = new MemoryStream(data);
        return Service.UploadAsync(
            tripId ?? Trip,
            new PhotoUploadRequest(stream, fileName, data.Length, markerId),
            CancellationToken.None);
    }

    /// <summary>Puts a photo straight into the repository, skipping the upload rules.</summary>
    public Photo Seed(Guid? markerId = null, long sizeBytes = 100, Guid? ownerId = null, Guid? tripId = null)
    {
        var photo = Photo.Create(
            tripId ?? Trip,
            markerId,
            ownerId ?? Owner,
            "seed.jpg",
            ImageKind.Jpeg,
            sizeBytes,
            ImageFacts.Unknown,
            hasThumbnail: false,
            DateTimeOffset.UtcNow);

        Repository.Seed(photo);
        return photo;
    }

    public async Task<byte[]> ReadAllAsync(PhotoContent content)
    {
        await using var stream = content.Content;
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        return buffer.ToArray();
    }
}
