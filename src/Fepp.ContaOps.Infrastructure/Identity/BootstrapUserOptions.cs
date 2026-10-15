using System.ComponentModel.DataAnnotations;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Configuración opcional del usuario regular inicial.
/// </summary>
public sealed class BootstrapUserOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "BootstrapUser";

    /// <summary>
    /// Correo del usuario regular inicial.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario regular inicial.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indica si se configuró el usuario regular inicial.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) || !string.IsNullOrWhiteSpace(Password);

    /// <summary>
    /// Valida la configuración cuando fue proporcionada.
    /// </summary>
    /// <returns><see langword="true"/> si está completa o ausente.</returns>
    public bool IsValid()
    {
        return !IsConfigured
            || new EmailAddressAttribute().IsValid(Email)
                && !string.IsNullOrWhiteSpace(Password);
    }
}
