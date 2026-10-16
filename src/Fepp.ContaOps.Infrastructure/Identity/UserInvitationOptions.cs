namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Configuración del ciclo de vida y destino de las invitaciones.
/// </summary>
public sealed class UserInvitationOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "UserInvitation";

    /// <summary>
    /// Duración del token de activación en horas.
    /// </summary>
    public double TokenLifetimeHours { get; set; } = 24;

    /// <summary>
    /// URL HTTPS de la interfaz cliente que procesará la activación.
    /// </summary>
    public string ActivationBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Indica si las opciones cumplen las reglas de seguridad de invitaciones.
    /// </summary>
    /// <returns><see langword="true"/> cuando la vigencia y la URL son válidas.</returns>
    public bool IsValid()
    {
        return double.IsFinite(TokenLifetimeHours)
            && TokenLifetimeHours > 0
            && Uri.TryCreate(ActivationBaseUrl, UriKind.Absolute, out Uri? activationUri)
            && string.Equals(activationUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
