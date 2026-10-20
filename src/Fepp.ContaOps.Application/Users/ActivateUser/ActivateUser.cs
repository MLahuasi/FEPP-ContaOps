using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Users.ActivateUser;

/// <summary>
/// Coordina la activación de una cuenta mediante su invitación.
/// </summary>
public sealed class ActivateUser
{
    /// <summary>
    /// Servicio que valida la invitación y establece la contraseña inicial.
    /// </summary>
    private readonly IUserInvitationService _invitationService;

    /// <summary>
    /// Inicializa el caso de uso de activación.
    /// </summary>
    /// <param name="invitationService">Servicio del ciclo de invitaciones.</param>
    public ActivateUser(IUserInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    /// <summary>
    /// Activa una cuenta pendiente con un token válido.
    /// </summary>
    /// <param name="input">Datos de activación de la cuenta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>El usuario activado o un error esperado.</returns>
    public Task<Result<User>> ExecuteAsync(
        ActivateUserInput input,
        CancellationToken cancellationToken = default)
    {
        return _invitationService.ActivateAsync(
            input.UserId,
            input.Token,
            input.NewPassword,
            cancellationToken);
    }
}
