using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Security;
using TripTracker.SharedKernel.TripTracker.Framework;

namespace TripTracker.Identity.Application;

/// <summary>
/// Creates the first administrator at start-up when <c>Identity:BootstrapAdmin</c> is configured and no account
/// with that e-mail exists. The database must already have the identity schema (apply the migrations first).
/// </summary>
internal sealed class BootstrapAdminService : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly BootstrapAdminOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<BootstrapAdminService> _logger;

    public BootstrapAdminService(
        IServiceScopeFactory scopes,
        IOptions<BootstrapAdminOptions> options,
        TimeProvider clock,
        ILogger<BootstrapAdminService> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (await users.FindByEmailAsync(_options.Email) is not null)
        {
            return;
        }

        var admin = AppUser.Create(
            _options.Email.Trim(),
            _options.DisplayName.Trim(),
            RoleNames.ADMIN,
            _clock.GetUtcNow(),
            emailConfirmed: true);

        var result = await users.CreateAsync(admin, _options.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Created the bootstrap administrator {Email}.", admin.Email);
        }
        else
        {
            _logger.LogError(
                "Could not create the bootstrap administrator: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
