using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using TripTracker.Identity.Contracts;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Persistence;
using TripTracker.Identity.Security;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Identity;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.Identity.Tests.Fakes;

/// <summary>
/// Wires the real Identity services to an in-memory database, a hand-moved clock and a fake Google validator,
/// all in one scope, the way one request would see them.
/// </summary>
internal sealed class IdentityTestBed : IDisposable
{
    public const string Password = "Passw0rd";
    public const string Email = "traveller@example.com";

    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public IdentityTestBed()
    {
        Clock = new FakeClock(DateTimeOffset.UtcNow);
        User = new FakeCurrentUser();
        Google = new FakeGoogleTokenValidator();
        Jwt = new JwtOptions { SigningKey = new string('k', 48) };

        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(User);
        services.AddSingleton<TimeProvider>(Clock);
        IdentityModule.Register(services, Jwt, new GoogleOptions(), db => db.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IGoogleTokenValidator>(Google);

        _provider = services.BuildServiceProvider(validateScopes: true);
        _scope = _provider.CreateScope();
    }

    public FakeClock Clock { get; }

    public FakeCurrentUser User { get; }

    public FakeGoogleTokenValidator Google { get; }

    public JwtOptions Jwt { get; }

    public IAuthService Auth => _scope.ServiceProvider.GetRequiredService<IAuthService>();

    public IUserManagement Users => _scope.ServiceProvider.GetRequiredService<IUserManagement>();

    public UserManager<AppUser> Manager => _scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

    public ITokenService Tokens => _scope.ServiceProvider.GetRequiredService<ITokenService>();

    public IServiceScopeFactory Scopes => _provider.GetRequiredService<IServiceScopeFactory>();

    public Task<Result<AuthResponse, Error>> RegisterAsync(
        string email = Email,
        string password = Password,
        string displayName = "Traveller") =>
        Auth.RegisterAsync(new RegisterRequest(email, password, displayName), CancellationToken.None);

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
