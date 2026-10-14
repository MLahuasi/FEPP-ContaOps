using FluentValidation;
using Fepp.ContaOps.Api.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
}
