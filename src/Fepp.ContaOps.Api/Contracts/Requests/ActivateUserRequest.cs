namespace Fepp.ContaOps.Api.Contracts.Requests;

/// <summary>
/// Datos necesarios para activar una cuenta mediante su invitación.
/// </summary>
public sealed record ActivateUserRequest
{
    /// <summary>
    /// Identificador del usuario asociado al token.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Token opaco de activación recibido por el usuario.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Nueva contraseña de la cuenta.
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;
}
