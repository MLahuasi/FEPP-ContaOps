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
    /// <returns>Identificador de la nueva sesión y refresh token inicial.</returns>
    Task<UserSessionResult> ReplaceActiveSessionAsync(
        User user,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de la creación de una sesión para asociarla al JWT y entregar el refresh token.
/// </summary>
/// <param name="SessionId">Identificador persistido de la sesión.</param>
/// <param name="RefreshToken">Valor plano del refresh token disponible solo para la respuesta de login.</param>
public sealed record UserSessionResult(Guid SessionId, string RefreshToken);
