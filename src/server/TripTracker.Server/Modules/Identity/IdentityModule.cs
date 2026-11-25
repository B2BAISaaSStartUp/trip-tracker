using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TripTracker.Identity.Application;
using TripTracker.Identity.Contracts;
using TripTracker.Identity.Domain;
using TripTracker.Identity.Persistence;
using TripTracker.Identity.Security;

namespace TripTracker.Identity;

/// <summary>Registers the Identity module: accounts, JWT authentication and Google sign-in.</summary>
public static class IdentityModule
{
    /// <summary>Name of the connection string used by the Identity module.</summary>
    public const string ConnectionStringName = "Identity";

    internal const string MigrationsHistoryTable = "__ef_migrations_history";

    private const string JwtSection = "Identity:Jwt";
    private const string GoogleSection = "Identity:Google";
    private const string BootstrapAdminSection = "Identity:BootstrapAdmin";

    /// <summary>
    /// Registers Identity services and JWT bearer authentication. Reads <c>ConnectionStrings:Identity</c> and
    /// <c>Identity:Jwt</c> (including <c>SigningKey</c>) from configuration - user secrets or environment
    /// variables in development; never commit them. The host must also call <c>UseAuthentication()</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The connection string or the JWT settings are missing or invalid.</exception>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");
        }

        var jwt = configuration.GetSection(JwtSection).Get<JwtOptions>() ?? new JwtOptions();
        jwt.Validate();

        var google = configuration.GetSection(GoogleSection).Get<GoogleOptions>() ?? new GoogleOptions();

        Register(
            services,
            jwt,
            google,
            db => db.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, IdentityStoreDbContext.SchemaName)));

        var admin = configuration.GetSection(BootstrapAdminSection).Get<BootstrapAdminOptions>();
        if (admin is not null
            && !string.IsNullOrWhiteSpace(admin.Email)
            && !string.IsNullOrWhiteSpace(admin.Password))
        {
            services.AddSingleton(Options.Create(admin));
            services.AddHostedService<BootstrapAdminService>();
        }

        return services;
    }

    /// <summary>Wires everything except the database provider, which tests replace with an in-memory one.</summary>
    internal static void Register(
        IServiceCollection services,
        JwtOptions jwt,
        GoogleOptions google,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<IdentityStoreDbContext>(configureDatabase);

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // The e-mail is the user name and is validated separately, so do not restrict its characters.
                options.User.AllowedUserNameCharacters = string.Empty;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<IdentityStoreDbContext>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(Options.Create(jwt));
        services.AddSingleton(Options.Create(google));
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserManagement, UserManagementService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var settings = jwtOptions.Value;

                // Keep claim names as issued ("sub", "role") instead of mapping them to the old WS-* URIs.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });
    }
}
