using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para consultar usuarios autenticables.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Busca un usuario por correo electrónico.
    /// </summary>
    /// <param name="email">Correo electrónico a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El usuario encontrado o <see langword="null"/>.</returns>
    Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un usuario por su identificador técnico.
    /// </summary>
    /// <param name="userId">Identificador técnico del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Usuario encontrado o <see langword="null"/>.</returns>
    Task<User?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
