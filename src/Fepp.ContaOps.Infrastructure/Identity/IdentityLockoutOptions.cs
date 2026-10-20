namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Configuración de bloqueo de cuentas de ASP.NET Core Identity.
/// </summary>
public sealed class IdentityLockoutOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "Identity:Lockout";

    /// <summary>
    /// Indica si las cuentas nuevas pueden bloquearse.
    /// </summary>
    public bool AllowedForNewUsers { get; set; } = true;

    /// <summary>
    /// Número máximo de intentos fallidos antes del bloqueo.
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Duración del bloqueo en minutos.
    /// </summary>
    public int DefaultLockoutTimeSpanMinutes { get; set; } = 15;

    /// <summary>
    /// Indica si la configuración tiene valores compatibles con Identity.
    /// </summary>
    /// <returns><see langword="true"/> cuando los límites son válidos.</returns>
    public bool IsValid()
    {
        return MaxFailedAccessAttempts > 0
            && DefaultLockoutTimeSpanMinutes > 0;
    }
}
