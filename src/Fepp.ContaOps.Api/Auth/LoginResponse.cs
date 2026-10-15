namespace Fepp.ContaOps.Api.Auth;

/// <summary>
/// Respuesta de un inicio de sesión exitoso.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn);
