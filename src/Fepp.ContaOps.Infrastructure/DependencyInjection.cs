using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure.Email;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Persistence;
using Fepp.ContaOps.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

namespace Fepp.ContaOps.Infrastructure;

/// <summary>
/// Registra los adaptadores de infraestructura.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los adaptadores de infraestructura al contenedor de dependencias.
    /// </summary>
    /// <param name="services">Contenedor de servicios de la aplicación.</param>
    /// <param name="configuration">Configuración del proceso anfitrión.</param>
    /// <returns>El mismo contenedor para permitir encadenar configuraciones.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default must be configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddOptions<UserInvitationOptions>()
            .Bind(configuration.GetSection(UserInvitationOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "UserInvitation:TokenLifetimeHours must be finite and greater than zero, and UserInvitation:ActivationBaseUrl must be an HTTPS URL.")
            .ValidateOnStart();

        string? keyRingPath = configuration["DataProtection:KeyRingPath"];
        IDataProtectionBuilder dataProtectionBuilder = services.AddDataProtection();
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            DirectoryInfo keyRingDirectory = new(keyRingPath);
            keyRingDirectory.Create();
            dataProtectionBuilder.PersistKeysToFileSystem(keyRingDirectory);
            if (OperatingSystem.IsWindows())
            {
                dataProtectionBuilder.ProtectKeysWithDpapi();
            }
        }

        services.AddOptions<UserInvitationTokenProviderOptions>()
            .Configure<IOptions<UserInvitationOptions>>((tokenOptions, invitationOptions) =>
            {
                tokenOptions.TokenLifespan = TimeSpan.FromHours(
                    invitationOptions.Value.TokenLifetimeHours);
            });
        services.AddOptions<EmailSmtpOptions>()
            .Bind(configuration.GetSection(EmailSmtpOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "Email:Smtp must configure a valid encrypted SMTP connection, credentials, sender, and positive timeout.")
            .ValidateOnStart();
        services.AddOptions<EmailTemplatesOptions>()
            .Bind(configuration.GetSection(EmailTemplatesOptions.SectionName));
        services.AddIdentity<ApplicationIdentityUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 6;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddTokenProvider<UserInvitationTokenProvider>(
                IdentityUserInvitationService.TokenProviderName)
            .AddDefaultTokenProviders();

        services.AddScoped<IUserRepository, IdentityUserRepository>();
        services.AddScoped<IUserCredentialService, IdentityCredentialService>();
        services.AddScoped<IUserInvitationService, IdentityUserInvitationService>();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer must be configured.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience must be configured.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SigningKey)
                    && Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey must contain at least 32 UTF-8 bytes.")
            .Validate(
                options => options.AccessTokenLifetimeMinutes > 0,
                "Jwt:AccessTokenLifetimeMinutes must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "BootstrapAdmin:Email and BootstrapAdmin:Password must be configured with valid values.")
            .ValidateOnStart();
        services.AddOptions<BootstrapUserOptions>()
            .Bind(configuration.GetSection(BootstrapUserOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "BootstrapUser:Email and BootstrapUser:Password must both be configured when a regular user is enabled.")
            .ValidateOnStart();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IdentitySeeder>();
        return services;
    }
}
