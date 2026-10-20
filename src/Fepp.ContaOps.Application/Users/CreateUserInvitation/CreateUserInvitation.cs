using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;

namespace Fepp.ContaOps.Application.Users.CreateUserInvitation;

/// <summary>
/// Coordina el registro de una cuenta pendiente y el envío de su invitación.
/// </summary>
public sealed class CreateUserInvitation
{
    /// <summary>
    /// Servicio que registra la cuenta y genera la invitación.
    /// </summary>
    private readonly IUserInvitationService _invitationService;

    /// <summary>
    /// Notificador que entrega la invitación al usuario.
    /// </summary>
    private readonly IInvitationNotifier _invitationNotifier;

    /// <summary>
    /// Inicializa el caso de uso de creación de invitaciones.
    /// </summary>
    /// <param name="invitationService">Servicio del ciclo de invitaciones.</param>
    /// <param name="invitationNotifier">Notificador de invitaciones.</param>
    public CreateUserInvitation(
        IUserInvitationService invitationService,
        IInvitationNotifier invitationNotifier)
    {
        _invitationService = invitationService;
        _invitationNotifier = invitationNotifier;
    }

    /// <summary>
    /// Registra la cuenta pendiente y solicita el envío de su invitación.
    /// </summary>
    /// <param name="input">Datos de la cuenta que se desea invitar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La invitación interna generada o un error esperado.</returns>
    public async Task<Result<UserInvitationResult>> ExecuteAsync(
        CreateUserInvitationInput input,
        CancellationToken cancellationToken = default)
    {
        Result<UserInvitationResult> result = await _invitationService.RegisterPendingAsync(
            input.Email,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result;
        }

        UserInvitationResult invitation = result.Value!;
        await _invitationNotifier.NotifyAsync(invitation, cancellationToken);
        return Result<UserInvitationResult>.Success(invitation);
    }
}
