using System.ComponentModel.DataAnnotations;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Configuración segura del administrador inicial.
/// </summary>
public sealed class BootstrapAdminOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "BootstrapAdmin";

    /// <summary>
    /// Correo del administrador inicial.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña inicial del administrador.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Valida que el correo y la contraseña provengan de una configuración usable.
    /// </summary>
    /// <returns>Resultado de validación de la configuración.</returns>
    public bool IsValid()
    {
        return new EmailAddressAttribute().IsValid(Email)
            && !string.IsNullOrWhiteSpace(Password);
    }
}
