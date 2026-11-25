using TripTracker.Web.Services;

namespace TripTracker.Web.Tests.Architecture;

public class ProjectReferenceTests
{
    [Fact]
    public void Web_OfTheTripTrackerAssemblies_ReferencesOnlySharedKernel()
    {
        var references = typeof(ApiClient).Assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("TripTracker.", StringComparison.Ordinal));

        Assert.Equal(["TripTracker.SharedKernel"], references);
    }
}
