using TripTracker.SharedKernel.TripTracker.Core;

namespace TripTracker.Identity.Tests.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; }

    public bool IsAuthenticated { get; set; }

    public void SignInAs(Guid userId)
    {
        UserId = userId;
        IsAuthenticated = true;
    }

    public void SignOut()
    {
        UserId = Guid.Empty;
        IsAuthenticated = false;
    }
}
