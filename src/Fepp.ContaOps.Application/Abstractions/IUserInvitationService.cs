using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para coordinar el ciclo de vida de las invitaciones de usuarios.
/// </summary>
public interface IUserInvitationService
{
    /// <summary>
    /// Registra una cuenta pendiente y genera su invitación.
    /// </summary>
    /// <param name="email">Correo validado de la cuenta que se registra.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Usuario pendiente y token de activación, o un error esperado.</returns>
    Task<Result<UserInvitationResult>> RegisterPendingAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activa una cuenta pendiente estableciendo su primera contraseña.
    /// </summary>
    /// <param name="userId">Identificador de la cuenta que se activa.</param>
    /// <param name="token">Token dedicado de activación.</param>
    /// <param name="newPassword">Primera contraseña de la cuenta.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Usuario activo o el error neutral de token inválido.</returns>
    Task<Result<User>> ActivateAsync(
        UserId userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera una nueva invitación para una cuenta pendiente.
    /// </summary>
    /// <param name="userId">Identificador de la cuenta pendiente.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Usuario pendiente y nuevo token, o un error esperado.</returns>
    Task<Result<UserInvitationResult>> ResendAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
