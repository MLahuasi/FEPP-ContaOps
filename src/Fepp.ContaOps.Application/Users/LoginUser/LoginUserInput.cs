namespace Fepp.ContaOps.Application.Users.LoginUser;

/// <summary>
/// Datos de entrada del caso de uso de inicio de sesión.
/// </summary>
public sealed record LoginUserInput(
    string Email,
    string Password);
