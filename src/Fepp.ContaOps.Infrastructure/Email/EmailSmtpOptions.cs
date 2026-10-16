using System.ComponentModel.DataAnnotations;

namespace Fepp.ContaOps.Infrastructure.Email;

/// <summary>
/// Configuración neutral del transporte SMTP.
/// </summary>
public sealed class EmailSmtpOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "Email:Smtp";

    /// <summary>Host del proveedor SMTP.</summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>Puerto del proveedor SMTP.</summary>
    public int Port { get; set; }

    /// <summary>Modo TLS utilizado por la conexión.</summary>
    public SmtpSecurityMode SecurityMode { get; set; }

    /// <summary>Cuenta autorizada para autenticarse ante SMTP.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Contraseña SMTP o contraseña de aplicación.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Dirección autorizada que aparecerá como remitente.</summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Nombre visible opcional del remitente.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de espera de operaciones SMTP en segundos.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Indica si la configuración SMTP cumple las restricciones del transporte.
    /// </summary>
    /// <returns><see langword="true"/> cuando todos los valores son utilizables.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Host)
            && Port > 0
            && IsCompatiblePort()
            && !string.IsNullOrWhiteSpace(Username)
            && !string.IsNullOrWhiteSpace(Password)
            && new EmailAddressAttribute().IsValid(FromAddress)
            && TimeoutSeconds > 0;
    }

    /// <summary>
    /// Comprueba que el puerto corresponda al modo TLS configurado.
    /// </summary>
    /// <returns><see langword="true"/> cuando no se solicita SMTP sin cifrado.</returns>
    private bool IsCompatiblePort()
    {
        return SecurityMode switch
        {
            SmtpSecurityMode.StartTls => Port == 587,
            SmtpSecurityMode.TlsOnConnect => Port == 465,
            _ => false
        };
    }
}
