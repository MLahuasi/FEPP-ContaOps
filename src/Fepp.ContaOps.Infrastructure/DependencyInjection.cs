using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure.Email;
using Fepp.ContaOps.Infrastructure.Email.Templates;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Persistence;
using Fepp.ContaOps.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text;

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
        services.AddOptions<IdentityPasswordOptions>()
            .Bind(configuration.GetSection(IdentityPasswordOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "Identity:Password contains invalid password policy values.")
            .ValidateOnStart();
        services.AddOptions<IdentityLockoutOptions>()
            .Bind(configuration.GetSection(IdentityLockoutOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "Identity:Lockout contains invalid lockout policy values.")
            .ValidateOnStart();

        services.AddOptions<EmailSmtpOptions>()
            .Bind(configuration.GetSection(EmailSmtpOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "Email:Smtp must configure a valid encrypted SMTP connection, credentials, sender, and positive timeout.")
            .ValidateOnStart();
        services.AddOptions<EmailTemplatesOptions>()
            .Bind(configuration.GetSection(EmailTemplatesOptions.SectionName));
        services.AddSingleton<InvitationTemplateCatalog>();
        services.AddSingleton<InvitationTemplateRenderer>();
        services.AddScoped<IInvitationNotifier, InvitationEmailNotifier>();
        services.AddSingleton<IEmailSender, MailKitSmtpEmailSender>();
        services.AddIdentity<ApplicationIdentityUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddTokenProvider<HashedUserTokenProvider>(
                IdentityUserInvitationService.TokenProviderName);
        services.AddOptions<IdentityOptions>()
            .Configure<IOptions<IdentityPasswordOptions>, IOptions<IdentityLockoutOptions>>(
                (identityOptions, passwordOptions, lockoutOptions) =>
                {
                    identityOptions.Password.RequiredLength = passwordOptions.Value.RequiredLength;
                    identityOptions.Password.RequireUppercase = passwordOptions.Value.RequireUppercase;
                    identityOptions.Password.RequireLowercase = passwordOptions.Value.RequireLowercase;
                    identityOptions.Password.RequireDigit = passwordOptions.Value.RequireDigit;
                    identityOptions.Password.RequireNonAlphanumeric = passwordOptions.Value.RequireNonAlphanumeric;
                    identityOptions.Password.RequiredUniqueChars = passwordOptions.Value.RequiredUniqueChars;
                    identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                        lockoutOptions.Value.DefaultLockoutTimeSpanMinutes);
                    identityOptions.Lockout.MaxFailedAccessAttempts = lockoutOptions.Value.MaxFailedAccessAttempts;
                    identityOptions.Lockout.AllowedForNewUsers = lockoutOptions.Value.AllowedForNewUsers;
                });

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
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<IOpaqueTokenGenerator, OpaqueTokenGenerator>();
        services.AddSingleton<OpaqueTokenCryptography>();
        services.AddScoped<IOpaqueTokenStore, OpaqueTokenStore>();
        services.AddScoped<UserSessionStore>();
        services.AddScoped<IUserSessionStore>(
            serviceProvider => serviceProvider.GetRequiredService<UserSessionStore>());
        services.AddScoped<IdentitySeeder>();
        return services;
    }
}
