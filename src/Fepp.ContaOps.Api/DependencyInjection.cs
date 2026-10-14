using FluentValidation;
using Fepp.ContaOps.Api.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System.Reflection;

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
    /// <returns>El mismo contenedor para permitir encadenar configuraciones.</returns>
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ValidationFilter>();
        services.AddControllers(options => options.Filters.AddService<ValidationFilter>());
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
                Description = "API para los procesos operativos contables de FEPP."
            });

            string documentationFile = Path.Combine(
                AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(documentationFile))
            {
                options.IncludeXmlComments(documentationFile);
            }
        });

        return services;
    }
}
