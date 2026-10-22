using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para emitir tokens de acceso.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>
    /// Emite un token para un usuario autenticado.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="sessionId">Identificador de la sesión asociada al token.</param>
    /// <param name="jti">Identificador único del token.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de emisión del token.</returns>
    Task<TokenResult> IssueAsync(
        User user,
        Guid sessionId,
        string jti,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado independiente de la tecnología de emisión de tokens.
/// </summary>
/// <param name="AccessToken">Token de acceso.</param>
/// <param name="ExpiresIn">Duración del token en segundos.</param>
/// <param name="RefreshToken">Token de renovación asociado a la sesión.</param>
public sealed record TokenResult(string AccessToken, long ExpiresIn, string RefreshToken = "");
