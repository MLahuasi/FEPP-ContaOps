using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para crear y reemplazar la sesión persistida de un usuario.
/// </summary>
public interface IUserSessionStore
{
    /// <summary>
    /// Revoca persistentemente una sesión autenticada.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="revokedAtUtc">Instante UTC de revocación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la revocación.</returns>
    Task RevokeSessionAsync(
        Guid sessionId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rota un refresh token opaco sin requerir que el cliente conozca la sesión.
    /// </summary>
    /// <param name="refreshToken">Valor plano presentado por el cliente.</param>
    /// <param name="nowUtc">Instante UTC usado para evaluar la vigencia.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Resultado neutral de la rotación.</returns>
    Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(
        string refreshToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

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

/// <summary>
/// Estados públicos para el caso de uso de rotación.
/// </summary>
public enum RefreshTokenRotationStatus
{
    /// <summary>La rotación fue exitosa.</summary>
    Rotated = 1,
    /// <summary>El token no es válido.</summary>
    Invalid = 2,
    /// <summary>El token ya fue utilizado o revocado.</summary>
    Reused = 3,
    /// <summary>La sesión o el token expiraron.</summary>
    Expired = 4
}

/// <summary>
/// Resultado de rotar un refresh token sin exponer datos persistentes.
/// </summary>
/// <param name="Status">Estado de la operación.</param>
/// <param name="UserId">Identificador técnico del usuario, solo para continuar el caso de uso.</param>
/// <param name="SessionId">Sesión asociada, solo para emitir el JWT.</param>
/// <param name="RefreshToken">Nuevo token plano cuando la rotación fue exitosa.</param>
public sealed record RefreshTokenRotationResult(
    RefreshTokenRotationStatus Status,
    string? UserId,
    Guid? SessionId,
    string? RefreshToken);
