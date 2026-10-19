namespace Fepp.ContaOps.Application.Users.CreateUserInvitation;

/// <summary>
/// Datos de entrada del caso de uso de creación de invitaciones.
/// </summary>
/// <param name="Email">Correo electrónico de la cuenta que se desea invitar.</param>
public sealed record CreateUserInvitationInput(string Email);
