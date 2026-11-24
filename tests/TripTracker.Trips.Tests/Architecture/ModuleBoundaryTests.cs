using System.Reflection;
using TripTracker.Trips.Contracts;

namespace TripTracker.Trips.Tests.Architecture;

public class ModuleBoundaryTests
{
    private static readonly Assembly TripsAssembly = typeof(TripsModule).Assembly;

    [Fact]
    public void Trips_DoesNotReferenceApiDataAccessOrOtherModules()
    {
        var allowed = new HashSet<string> { "TripTracker.SharedKernel", "TripTracker.Trips" };

        var forbidden = TripsAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("TripTracker.", StringComparison.Ordinal) && !allowed.Contains(n))
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Trips_ExposesOnlyContractsAndRegistration()
    {
        var expected = new HashSet<Type>
        {
            typeof(ITripService),
            typeof(ITripAccess),
            typeof(ITripModeration),
            typeof(ITripDeletionHook),
            typeof(TripAccessKind),
            typeof(TripAccessRequest),
            typeof(TripsModule),
        };

        var exported = TripsAssembly.GetExportedTypes().ToHashSet();

        Assert.True(
            exported.SetEquals(expected),
            $"Unexpected public types: {string.Join(", ", exported.Except(expected).Select(t => t.FullName))}; "
            + $"missing: {string.Join(", ", expected.Except(exported).Select(t => t.FullName))}");
    }
}
