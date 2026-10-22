namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para consultar los roles actuales de un usuario autenticado.
/// </summary>
public interface IUserRoleReader
{
    /// <summary>
    /// Obtiene los roles vigentes del usuario.
    /// </summary>
    /// <param name="userId">Identificador técnico del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Roles actuales o <see langword="null"/> si el usuario no existe.</returns>
    Task<IReadOnlyCollection<string>?> GetRolesAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
