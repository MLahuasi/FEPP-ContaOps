namespace Fepp.ContaOps.Api.Auth;

/// <summary>
/// Datos de credenciales enviados al endpoint de inicio de sesión.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Contraseña del usuario.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
