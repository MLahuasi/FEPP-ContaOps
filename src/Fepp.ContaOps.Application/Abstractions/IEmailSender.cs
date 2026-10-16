namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto de transporte de mensajes de correo.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Entrega un mensaje mediante el proveedor configurado.
    /// </summary>
    /// <param name="message">Mensaje neutral que se debe entregar.</param>
    /// <param name="cancellationToken">Token de cancelación del envío.</param>
    /// <returns>Una operación que termina cuando el transporte responde.</returns>
    Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default);
}
