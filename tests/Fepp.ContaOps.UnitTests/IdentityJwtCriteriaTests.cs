using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fepp.ContaOps.Api;
using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Security;
using Fepp.ContaOps.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Valida los criterios de configuración, emisión y autorización de SPEC 02
/// sin depender de una base de datos.
/// </summary>
public sealed class IdentityJwtCriteriaTests
{
    /// <summary>
    /// Comprueba que un token administrativo contiene los metadatos y el rol esperados.
    /// </summary>
    [Fact]
    public async Task JwtIssuer_EmitsAdminTokenWithExpectedClaims()
    {
        JwtTokenIssuer issuer = new(Options.Create(CreateJwtOptions()));
        User user = new(new UserId("admin-id"), "admin@example.com", [IdentitySeeder.AdminRole]);

        TokenResult result = await issuer.IssueAsync(user, Guid.NewGuid(), Guid.NewGuid().ToString("N"));
        JwtSecurityToken token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal("FEPP-ContaOps.Tests", token.Issuer);
        Assert.Contains("FEPP-ContaOps.Tests.Api", token.Audiences);
        Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == IdentitySeeder.AdminRole);
        Assert.True(token.ValidTo > DateTime.UtcNow);
        Assert.Equal(3600, result.ExpiresIn);
    }

    /// <summary>
    /// Comprueba que la validación rechaza firma, issuer y audience incorrectos.
    /// </summary>
    [Fact]
    public async Task JwtValidation_RejectsIncorrectSignatureIssuerAndAudience()
    {
        JwtOptions options = CreateJwtOptions();
        JwtTokenIssuer issuer = new(Options.Create(options));
        User user = new(new UserId("user-id"), "user@example.com", [IdentitySeeder.UserRole]);
        string accessToken = (await issuer.IssueAsync(
            user,
            Guid.NewGuid(),
            Guid.NewGuid().ToString("N"))).AccessToken;

        JwtOptions incorrectKey = CreateJwtOptions();
        incorrectKey.SigningKey = "different-signing-key-with-32-bytes!!";
        JwtOptions incorrectIssuer = CreateJwtOptions();
        incorrectIssuer.Issuer = "OtherIssuer";
        JwtOptions incorrectAudience = CreateJwtOptions();
        incorrectAudience.Audience = "OtherAudience";

        Assert.ThrowsAny<SecurityTokenException>(() => Validate(accessToken, incorrectKey));
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => Validate(accessToken, incorrectIssuer));
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => Validate(accessToken, incorrectAudience));
    }

    /// <summary>
    /// Comprueba que un token expirado es rechazado por la validación de lifetime.
    /// </summary>
    [Fact]
    public void JwtValidation_RejectsExpiredToken()
    {
        JwtOptions options = CreateJwtOptions();
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(options.SigningKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken expiredToken = new(
            issuer: options.Issuer,
            audience: options.Audience,
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: credentials);
        string serializedToken = new JwtSecurityTokenHandler().WriteToken(expiredToken);

        Assert.Throws<SecurityTokenExpiredException>(() => Validate(serializedToken, options));
    }

    /// <summary>
    /// Comprueba los valores de contraseña y bloqueo configurados para Identity.
    /// </summary>
    [Fact]
    public void IdentityOptions_UseSpecPasswordAndLockoutPolicy()
    {
        ServiceCollection services = new();
        services.AddInfrastructure(CreateConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider();
        IdentityOptions options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(12, options.Password.RequiredLength);
        Assert.True(options.Password.RequireUppercase);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireNonAlphanumeric);
        Assert.Equal(6, options.Password.RequiredUniqueChars);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.True(options.Lockout.AllowedForNewUsers);
    }

    /// <summary>
    /// Comprueba que la política AdminOnly exige exactamente el rol Admin.
    /// </summary>
    [Fact]
    public async Task AdminOnlyPolicy_RequiresAdminRole()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddApi(CreateConfiguration());
        using ServiceProvider provider = services.BuildServiceProvider();
        IAuthorizationPolicyProvider policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        AuthorizationPolicy policy = (await policyProvider.GetPolicyAsync("AdminOnly"))!;
        RolesAuthorizationRequirement requirement =
            Assert.Single(policy.Requirements.OfType<RolesAuthorizationRequirement>());

        Assert.Equal([IdentitySeeder.AdminRole], requirement.AllowedRoles);

        IAuthorizationService authorizationService = provider.GetRequiredService<IAuthorizationService>();
        ClaimsPrincipal admin = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentitySeeder.AdminRole)],
            "unit-test"));
        ClaimsPrincipal user = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentitySeeder.UserRole)],
            "unit-test"));

        Assert.True((await authorizationService.AuthorizeAsync(admin, "AdminOnly")).Succeeded);
        Assert.False((await authorizationService.AuthorizeAsync(user, "AdminOnly")).Succeeded);
    }

    /// <summary>
    /// Comprueba que las respuestas 401 y 403 contienen código y traceId.
    /// </summary>
    [Theory]
    [InlineData(StatusCodes.Status401Unauthorized, "authentication_required")]
    [InlineData(StatusCodes.Status403Forbidden, "forbidden")]
    public void ProblemDetailsFactory_IncludesCodeAndTraceId(int statusCode, string code)
    {
        DefaultHttpContext context = new();
        context.TraceIdentifier = "trace-test";
        context.Request.Path = "/test";

        ProblemDetails problemDetails = ProblemDetailsResponseFactory.Create(
            context,
            statusCode,
            "title",
            "detail",
            code);

        Assert.Equal(statusCode, problemDetails.Status);
        Assert.Equal(code, problemDetails.Extensions["code"]);
        Assert.Equal("trace-test", problemDetails.Extensions["traceId"]);
    }

    /// <summary>
    /// Comprueba que la configuración del administrador exige correo y contraseña.
    /// </summary>
    [Fact]
    public void BootstrapAdminOptions_RequiresEmailAndPassword()
    {
        BootstrapAdminOptions options = new();

        Assert.False(options.IsValid());
        options.Email = "admin@example.com";
        options.Password = "Password123!";
        Assert.True(options.IsValid());
    }

    /// <summary>
    /// Crea la configuración JWT y de conexión necesaria para registrar servicios.
    /// </summary>
    /// <returns>Configuración válida para las pruebas unitarias.</returns>
    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Jwt:Issuer"] = "FEPP-ContaOps.Tests",
                ["Jwt:Audience"] = "FEPP-ContaOps.Tests.Api",
                ["Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60"
            })
            .Build();
    }

    /// <summary>
    /// Crea las opciones JWT usadas por las pruebas de emisión y validación.
    /// </summary>
    /// <returns>Opciones JWT válidas.</returns>
    private static JwtOptions CreateJwtOptions()
    {
        return new JwtOptions
        {
            Issuer = "FEPP-ContaOps.Tests",
            Audience = "FEPP-ContaOps.Tests.Api",
            SigningKey = "test-signing-key-with-at-least-32-bytes",
            AccessTokenLifetimeMinutes = 60
        };
    }

    /// <summary>
    /// Valida un token con las reglas equivalentes a las de la API.
    /// </summary>
    /// <param name="accessToken">Token JWT que se debe validar.</param>
    /// <param name="options">Configuración esperada para la validación.</param>
    private static void Validate(string accessToken, JwtOptions options)
    {
        new JwtSecurityTokenHandler().ValidateToken(
            accessToken,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            },
            out _);
    }
}
