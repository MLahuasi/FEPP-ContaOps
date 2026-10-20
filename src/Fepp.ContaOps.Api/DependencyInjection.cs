using FluentValidation;
using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Reflection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Fepp.ContaOps.Api;

/// <summary>
/// Registra los servicios propios del adaptador HTTP.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega controllers, validadores y filtros HTTP al contenedor.
    /// </summary>
    /// <param name="services">Contenedor de servicios de la aplicación.</param>
    /// <param name="configuration">Configuración del proceso anfitrión.</param>
    /// <returns>El mismo contenedor para permitir encadenar configuraciones.</returns>
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ValidationFilter>();
        services.AddScoped<ApplicationErrorProblemDetailsMapper>();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
                ProblemDetailsResponseFactory.CreateModelStateValidationResult(
                    context.HttpContext,
                    context.ModelState);
        });
        services.AddControllers(options => options.Filters.AddService<ValidationFilter>());
        JwtOptions jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration must be configured.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteAuthenticationErrorAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "Authentication is required.",
                            "authentication_required");
                    },
                    OnForbidden = context => WriteAuthenticationErrorAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "You do not have permission to access this resource.",
                        "forbidden")
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(IdentitySeeder.AdminRole));
        });
        return services;
    }

    /// <summary>
    /// Registra el documento OpenAPI y los servicios de generación Swagger.
    /// </summary>
    /// <param name="services">Contenedor de servicios de la aplicación.</param>
    /// <returns>El mismo contenedor para permitir encadenar configuraciones.</returns>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FEPP-ContaOps API",
                Version = "v1",
                Description = "API para los procesos operativos contables de FEPP. "
                    + "Incluye autenticación JWT y el ciclo administrativo de invitaciones. "
                    + "Las respuestas de error usan application/problem+json con code y traceId."
            });

            string documentationFile = Path.Combine(
                AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
             if (File.Exists(documentationFile))
            {
                options.IncludeXmlComments(documentationFile);
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Introduzca el token JWT como: Bearer {token}."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        return services;
    }

    /// <summary>
    /// Escribe la respuesta uniforme para un challenge o forbid de autenticación.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud.</param>
    /// <param name="statusCode">Código HTTP de la respuesta.</param>
    /// <param name="detail">Detalle seguro para el cliente.</param>
    /// <param name="code">Código estable del error.</param>
    /// <returns>Una tarea que representa la escritura de la respuesta.</returns>
    private static Task WriteAuthenticationErrorAsync(
        HttpContext httpContext,
        int statusCode,
        string detail,
        string code)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        string responseBody = JsonSerializer.Serialize(
            ProblemDetailsResponseFactory.Create(
                httpContext,
                statusCode,
                statusCode == StatusCodes.Status401Unauthorized
                    ? "Authentication failed."
                    : "Access forbidden.",
                detail,
                code));
        return httpContext.Response.WriteAsync(responseBody);
    }
}
