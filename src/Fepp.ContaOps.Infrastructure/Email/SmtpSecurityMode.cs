namespace Fepp.ContaOps.Infrastructure.Email;

/// <summary>
/// Modo de cifrado obligatorio para una conexión SMTP.
/// </summary>
public enum SmtpSecurityMode
{
    /// <summary>
    /// Conexión que se actualiza mediante STARTTLS.
    /// </summary>
    StartTls,

    /// <summary>
    /// TLS activo desde el establecimiento de la conexión.
    /// </summary>
    TlsOnConnect
}
