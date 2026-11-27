using System.Reflection;
using TripTracker.Tracking.Contracts;

namespace TripTracker.Tracking.Tests.Architecture;

public class ModuleBoundaryTests
{
    private static readonly Assembly TrackingAssembly = typeof(TrackingModule).Assembly;

    [Fact]
    public void Tracking_DoesNotReferenceApiDataAccessOrOtherModules()
    {
        var allowed = new HashSet<string> { "TripTracker.SharedKernel", "TripTracker.Tracking" };

        var forbidden = TrackingAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("TripTracker.", StringComparison.Ordinal) && !allowed.Contains(n))
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Tracking_ExposesOnlyContractsAndRegistration()
    {
        var expected = new HashSet<Type>
        {
            typeof(ITrackingService),
            typeof(ITrackingTripCleanup),
            typeof(TrackingModule),
        };

        var exported = TrackingAssembly.GetExportedTypes().ToHashSet();

        Assert.True(
            exported.SetEquals(expected),
            $"Unexpected public types: {string.Join(", ", exported.Except(expected).Select(t => t.FullName))}; "
            + $"missing: {string.Join(", ", expected.Except(exported).Select(t => t.FullName))}");
    }
}
