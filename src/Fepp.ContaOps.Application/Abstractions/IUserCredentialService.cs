using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para comprobar credenciales sin acoplar Application a Identity.
/// </summary>
public interface IUserCredentialService
{
    /// <summary>
    /// Comprueba la contraseña de un usuario y aplica el bloqueo configurado.
    /// </summary>
    /// <param name="user">Usuario cuyas credenciales se comprueban.</param>
    /// <param name="password">Contraseña proporcionada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><see langword="true"/> cuando la contraseña es válida.</returns>
    Task<bool> VerifyPasswordAsync(
        User user,
        string password,
        CancellationToken cancellationToken = default);
}
