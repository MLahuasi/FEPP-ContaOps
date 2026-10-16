namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Indica que el transporte de correo no pudo confirmar la entrega.
/// </summary>
public sealed class EmailDeliveryUnavailableException : Exception
{
    /// <summary>
    /// Código estable para traducir el fallo a un error público seguro.
    /// </summary>
    public const string ErrorCode = "email_delivery_unavailable";

    /// <summary>
    /// Inicializa una excepción sin incluir detalles del proveedor o del destinatario.
    /// </summary>
    public EmailDeliveryUnavailableException()
        : base("The email delivery service is unavailable.")
    {
    }
}
