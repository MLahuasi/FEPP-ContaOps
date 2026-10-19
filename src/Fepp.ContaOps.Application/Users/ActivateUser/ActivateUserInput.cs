using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Users.ActivateUser;

/// <summary>
/// Datos de entrada del caso de uso de activación de usuarios.
/// </summary>
/// <param name="UserId">Identificador de la cuenta que se desea activar.</param>
/// <param name="Token">Token opaco de activación.</param>
/// <param name="NewPassword">Primera contraseña de la cuenta.</param>
public sealed record ActivateUserInput(
    UserId UserId,
    string Token,
    string NewPassword);
