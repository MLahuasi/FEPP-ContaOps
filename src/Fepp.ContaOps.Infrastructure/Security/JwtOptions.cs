namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Configuración necesaria para emitir tokens JWT.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Emisor que se incluirá en el token.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiencia que se incluirá en el token.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Clave secreta utilizada para firmar el token.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Duración del token en minutos.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
