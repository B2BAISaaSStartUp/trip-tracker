using TripTracker.Identity.Domain;
using TripTracker.Identity.Tests.Fakes;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Tests.Application;

public class UserManagementTests : IDisposable
{
    private readonly IdentityTestBed _bed = new();

    public void Dispose() => _bed.Dispose();

    [Fact]
    public async Task GetById_ReturnsTheAccount()
    {
        var registered = (await _bed.RegisterAsync()).Value;

        var result = await _bed.Users.GetByIdAsync(registered.User.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(IdentityTestBed.Email, result.Value.Email);
    }

    [Fact]
    public async Task GetById_ForAnUnknownId_IsNotFound()
    {
        var result = await _bed.Users.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND, "auth.user.not.found");
    }

    [Fact]
    public async Task Block_MarksTheAccountBlocked()
    {
        var id = (await _bed.RegisterAsync()).Value.User.Id;

        var blocked = await _bed.Users.BlockAsync(id, CancellationToken.None);

        Assert.True(blocked.IsSuccess);
        Assert.True((await _bed.Users.GetByIdAsync(id, CancellationToken.None)).Value.IsBlocked);
    }

    [Fact]
    public async Task Block_IsRepeatable()
    {
        var id = (await _bed.RegisterAsync()).Value.User.Id;
        await _bed.Users.BlockAsync(id, CancellationToken.None);

        var again = await _bed.Users.BlockAsync(id, CancellationToken.None);

        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task Block_OfAnAdministrator_IsAConflict()
    {
        var admin = AppUser.Create("admin@example.com", "Admin", RoleNames.ADMIN, DateTimeOffset.UtcNow);
        Assert.True((await _bed.Manager.CreateAsync(admin, IdentityTestBed.Password)).Succeeded);

        var result = await _bed.Users.BlockAsync(admin.Id, CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.CONFLICT, "auth.admin.cannot.be.blocked");
    }

    [Fact]
    public async Task Block_OfAnUnknownUser_IsNotFound()
    {
        var result = await _bed.Users.BlockAsync(Guid.NewGuid(), CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND);
    }

    [Fact]
    public async Task Unblock_OfAnActiveAccount_Succeeds()
    {
        var id = (await _bed.RegisterAsync()).Value.User.Id;

        var result = await _bed.Users.UnblockAsync(id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Unblock_OfAnUnknownUser_IsNotFound()
    {
        var result = await _bed.Users.UnblockAsync(Guid.NewGuid(), CancellationToken.None);

        ErrorAssert.Fails(result, ErrorType.NOT_FOUND);
    }
}
