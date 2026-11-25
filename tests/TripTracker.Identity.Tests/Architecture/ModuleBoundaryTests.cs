using System.Reflection;
using TripTracker.Identity.Contracts;

namespace TripTracker.Identity.Tests.Architecture;

public class ModuleBoundaryTests
{
    private static readonly Assembly IdentityAssembly = typeof(IdentityModule).Assembly;

    [Fact]
    public void Identity_DoesNotReferenceApiDataAccessOrOtherModules()
    {
        var allowed = new HashSet<string> { "TripTracker.SharedKernel", "TripTracker.Identity" };

        var forbidden = IdentityAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => n.StartsWith("TripTracker.", StringComparison.Ordinal) && !allowed.Contains(n))
            .ToList();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Identity_ExposesOnlyContractsAndRegistration()
    {
        var expected = new HashSet<Type>
        {
            typeof(IAuthService),
            typeof(IUserManagement),
            typeof(IdentityModule),
        };

        var exported = IdentityAssembly.GetExportedTypes().ToHashSet();

        Assert.True(
            exported.SetEquals(expected),
            $"Unexpected public types: {string.Join(", ", exported.Except(expected).Select(t => t.FullName))}; "
            + $"missing: {string.Join(", ", expected.Except(exported).Select(t => t.FullName))}");
    }
}
