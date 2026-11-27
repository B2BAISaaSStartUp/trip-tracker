using System.Reflection;
using TripTracker.Markers.Contracts;

namespace TripTracker.Markers.Tests.Architecture;

public class ModuleBoundaryTests
{
    private static readonly Assembly MarkersAssembly = typeof(MarkersModule).Assembly;

    [Fact]
    public void Markers_DoesNotReferenceApiDataAccessOrOtherModules()
    {
        var allowed = new HashSet<string> { "TripTracker.SharedKernel", "TripTracker.Markers" };

        var forbidden = MarkersAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("TripTracker.", StringComparison.Ordinal) && !allowed.Contains(n))
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Markers_ExposesOnlyContractsAndRegistration()
    {
        var expected = new HashSet<Type>
        {
            typeof(IMarkerService),
            typeof(IMarkerDeletionHook),
            typeof(IMarkerTripCleanup),
            typeof(MarkersModule),
        };

        var exported = MarkersAssembly.GetExportedTypes().ToHashSet();

        Assert.True(
            exported.SetEquals(expected),
            $"Unexpected public types: {string.Join(", ", exported.Except(expected).Select(t => t.FullName))}; "
            + $"missing: {string.Join(", ", expected.Except(exported).Select(t => t.FullName))}");
    }
}
