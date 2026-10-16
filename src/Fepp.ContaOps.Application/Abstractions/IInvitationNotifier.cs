namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para notificar una invitación sin conocer su transporte de correo.
/// </summary>
public interface IInvitationNotifier
{
    /// <summary>
    /// Construye y entrega la notificación de una invitación.
    /// </summary>
    /// <param name="invitation">Datos internos de la invitación.</param>
    /// <param name="cancellationToken">Token de cancelación del envío.</param>
    /// <returns>Una operación que termina cuando la notificación fue procesada.</returns>
    Task NotifyAsync(
        UserInvitationResult invitation,
        CancellationToken cancellationToken = default);
}
