using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Adapta las consultas de Identity al modelo de usuario de Domain.
/// </summary>
public sealed class IdentityUserRepository : IUserRepository
{
    /// <summary>
    /// Administrador de usuarios utilizado para consultar Identity.
    /// </summary>
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    /// <summary>
    /// Inicializa el repositorio.
    /// </summary>
    /// <param name="userManager">Administrador de usuarios de Identity.</param>
    public IdentityUserRepository(UserManager<ApplicationIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>
    /// Busca el usuario por correo y transforma sus datos y roles al modelo de Domain.
    /// </summary>
    /// <param name="email">Correo electrónico a consultar.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Usuario de Domain con sus roles efectivos, o <see langword="null"/> cuando no existe.
    /// </returns>
    public async Task<User?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? identityUser = await _userManager.FindByEmailAsync(email);
        if (identityUser is null)
        {
            return null;
        }

        IList<string> roles = await _userManager.GetRolesAsync(identityUser);
        return new User(
            new UserId(identityUser.Id),
            identityUser.Email ?? email,
            roles);
    }
}
