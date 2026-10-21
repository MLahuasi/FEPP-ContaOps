namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Resultado de crear una sesión de usuario y su refresh token inicial.
/// </summary>
/// <param name="SessionId">Identificador público de la sesión para asociar el JWT.</param>
/// <param name="RefreshToken">Valor plano del refresh token, disponible solo para el llamador.</param>
public sealed record UserSessionCreationResult(Guid SessionId, string RefreshToken);
