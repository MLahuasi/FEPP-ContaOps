namespace Fepp.ContaOps.Api.Contracts.Requests;

/// <summary>
/// Datos necesarios para crear una invitación de usuario.
/// </summary>
public sealed record CreateUserInvitationRequest
{
    /// <summary>
    /// Correo electrónico de la cuenta que se desea invitar.
    /// </summary>
    public string Email { get; init; } = string.Empty;
}
