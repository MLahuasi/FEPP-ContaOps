using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
