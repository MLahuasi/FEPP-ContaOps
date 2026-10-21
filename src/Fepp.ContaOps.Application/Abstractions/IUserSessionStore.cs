using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para crear y reemplazar la sesión persistida de un usuario.
/// </summary>
public interface IUserSessionStore
{
    /// <summary>
    /// Reemplaza la sesión activa del usuario después de validar sus credenciales.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="createdAtUtc">Instante UTC de creación.</param>
    /// <param name="expiresAtUtc">Instante UTC de expiración.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la nueva sesión.</returns>
    Task<UserSessionResult> ReplaceActiveSessionAsync(
        User user,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado mínimo de la creación de una sesión para asociarla al JWT.
/// </summary>
/// <param name="SessionId">Identificador persistido de la sesión.</param>
public sealed record UserSessionResult(Guid SessionId);
