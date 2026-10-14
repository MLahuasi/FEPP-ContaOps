using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Fepp.ContaOps.Api.Swagger;

/// <summary>
/// Determina si se exponen los endpoints de documentación Swagger.
/// </summary>
public static class SwaggerSettings
{
    /// <summary>
    /// Obtiene si Swagger está habilitado para el ambiente actual.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="environment">Ambiente de hosting actual.</param>
    /// <returns><see langword="true"/> si Swagger debe estar disponible.</returns>
    public static bool IsEnabled(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        string? configuredValue = configuration["Swagger:Enabled"];
        return bool.TryParse(configuredValue, out bool enabled)
            ? enabled
            : environment.IsDevelopment();
    }
}
