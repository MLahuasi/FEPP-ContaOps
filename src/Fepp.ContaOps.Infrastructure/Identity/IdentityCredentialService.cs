using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Comprueba contraseñas y aplica las reglas de bloqueo de Identity.
/// </summary>
public sealed class IdentityCredentialService : IUserCredentialService
{
    /// <summary>
    /// Administrador de usuarios utilizado para localizar la cuenta técnica.
    /// </summary>
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    /// <summary>
    /// Administrador de inicio de sesión utilizado para comprobar la contraseña y el bloqueo.
    /// </summary>
    private readonly SignInManager<ApplicationIdentityUser> _signInManager;

    /// <summary>
    /// Inicializa el adaptador de credenciales.
    /// </summary>
    /// <param name="userManager">Administrador de usuarios de Identity.</param>
    /// <param name="signInManager">Administrador de inicio de sesión de Identity.</param>
    public IdentityCredentialService(
        UserManager<ApplicationIdentityUser> userManager,
        SignInManager<ApplicationIdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    /// <summary>
    /// Busca el usuario técnico y comprueba la contraseña mediante Identity.
    /// </summary>
    /// <param name="user">Usuario de dominio cuyas credenciales se comprueban.</param>
    /// <param name="password">Contraseña proporcionada por el cliente.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// <see langword="true"/> únicamente cuando la contraseña es correcta y la cuenta
    /// no está bloqueada.
    /// </returns>
    public async Task<bool> VerifyPasswordAsync(
        User user,
        string password,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? identityUser = await _userManager.FindByIdAsync(user.Id.Value);
        if (identityUser is null)
        {
            return false;
        }

        SignInResult result = await _signInManager.CheckPasswordSignInAsync(
            identityUser,
            password,
            lockoutOnFailure: true);
        return result.Succeeded;
    }
}
