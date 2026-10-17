namespace Fepp.ContaOps.Api.Contracts.Responses;

/// <summary>
/// Representación pública de una cuenta creada mediante invitación.
/// </summary>
/// <param name="Id">Identificador del usuario creado.</param>
/// <param name="Email">Correo electrónico de la cuenta.</param>
/// <param name="Role">Rol inicial de la cuenta.</param>
public sealed record UserInvitationResponse(
    string Id,
    string Email,
    string Role);
