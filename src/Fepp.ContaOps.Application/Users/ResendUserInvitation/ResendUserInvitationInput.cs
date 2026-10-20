using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Users.ResendUserInvitation;

/// <summary>
/// Datos de entrada del caso de uso de reenvío de invitaciones.
/// </summary>
/// <param name="UserId">Identificador de la cuenta pendiente.</param>
public sealed record ResendUserInvitationInput(UserId UserId);
