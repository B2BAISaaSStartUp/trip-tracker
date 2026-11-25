using System.Reflection;
using TripTracker.Photos.Contracts;

namespace TripTracker.Photos.Tests.Architecture;

public class ModuleBoundaryTests
{
    private static readonly Assembly PhotosAssembly = typeof(PhotosModule).Assembly;

    [Fact]
    public void Photos_DoesNotReferenceApiDataAccessOrOtherModules()
    {
        var allowed = new HashSet<string> { "TripTracker.SharedKernel", "TripTracker.Photos" };

        var forbidden = PhotosAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("TripTracker.", StringComparison.Ordinal) && !allowed.Contains(n))
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Photos_ExposesOnlyContractsAndRegistration()
    {
        var expected = new HashSet<Type>
        {
            typeof(IPhotoService),
            typeof(IPhotoTripCleanup),
            typeof(PhotoUploadRequest),
            typeof(PhotoContent),
            typeof(PhotoVariant),
            typeof(PhotoLimits),
            typeof(PhotosModule),
        };

        var exported = PhotosAssembly.GetExportedTypes().ToHashSet();

        Assert.True(
            exported.SetEquals(expected),
            $"Unexpected public types: {string.Join(", ", exported.Except(expected).Select(t => t.FullName))}; "
            + $"missing: {string.Join(", ", expected.Except(exported).Select(t => t.FullName))}");
    }
}
