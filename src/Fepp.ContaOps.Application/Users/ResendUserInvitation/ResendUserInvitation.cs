using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;

namespace Fepp.ContaOps.Application.Users.ResendUserInvitation;

/// <summary>
/// Coordina la invalidación de una invitación y el envío de una nueva.
/// </summary>
public sealed class ResendUserInvitation
{
    /// <summary>
    /// Servicio que genera una nueva invitación para la cuenta pendiente.
    /// </summary>
    private readonly IUserInvitationService _invitationService;

    /// <summary>
    /// Notificador que entrega la nueva invitación al usuario.
    /// </summary>
    private readonly IInvitationNotifier _invitationNotifier;

    /// <summary>
    /// Inicializa el caso de uso de reenvío de invitaciones.
    /// </summary>
    /// <param name="invitationService">Servicio del ciclo de invitaciones.</param>
    /// <param name="invitationNotifier">Notificador de invitaciones.</param>
    public ResendUserInvitation(
        IUserInvitationService invitationService,
        IInvitationNotifier invitationNotifier)
    {
        _invitationService = invitationService;
        _invitationNotifier = invitationNotifier;
    }

    /// <summary>
    /// Genera una invitación nueva y solicita su envío.
    /// </summary>
    /// <param name="input">Identificador de la cuenta pendiente.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>La nueva invitación interna o un error esperado.</returns>
    public async Task<Result<UserInvitationResult>> ExecuteAsync(
        ResendUserInvitationInput input,
        CancellationToken cancellationToken = default)
    {
        Result<UserInvitationResult> result = await _invitationService.ResendAsync(
            input.UserId,
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
