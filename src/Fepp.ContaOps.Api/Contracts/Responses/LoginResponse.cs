namespace Fepp.ContaOps.Api.Contracts.Responses;

/// <summary>
/// Respuesta de un inicio de sesión exitoso.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn,
    string RefreshToken);
