using Fepp.ContaOps.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Adapta los roles de ASP.NET Core Identity al puerto de aplicación.
/// </summary>
public sealed class IdentityUserRoleReader : IUserRoleReader
{
    /// <summary>
    /// Administrador de usuarios utilizado para consultar los roles.
    /// </summary>
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    /// <summary>
    /// Inicializa el lector de roles.
    /// </summary>
    /// <param name="userManager">Administrador de usuarios de Identity.</param>
    public IdentityUserRoleReader(UserManager<ApplicationIdentityUser> userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>?> GetRolesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationIdentityUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        return roles.ToArray();
    }
}
