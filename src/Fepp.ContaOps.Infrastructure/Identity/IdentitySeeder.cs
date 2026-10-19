using Fepp.ContaOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Crea de forma idempotente los roles y el administrador inicial.
/// </summary>
public sealed class IdentitySeeder
{
    /// <summary>
    /// Rol administrativo de la aplicación.
    /// </summary>
    public const string AdminRole = "Admin";

    /// <summary>
    /// Rol de usuario de la aplicación.
    /// </summary>
    public const string UserRole = "User";

    /// <summary>
    /// Contexto de persistencia de Identity.
    /// </summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Administrador técnico de usuarios.
    /// </summary>
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    /// <summary>
    /// Administrador técnico de roles.
    /// </summary>
    private readonly RoleManager<IdentityRole> _roleManager;

    /// <summary>
    /// Configuración del administrador inicial.
    /// </summary>
    private readonly BootstrapAdminOptions _options;

    /// <summary>
    /// Inicializa el seed de Identity.
    /// </summary>
    /// <param name="dbContext">Contexto de persistencia.</param>
    /// <param name="userManager">Administrador de usuarios.</param>
    /// <param name="roleManager">Administrador de roles.</param>
    /// <param name="options">Configuración del administrador inicial.</param>
    public IdentitySeeder(
        AppDbContext dbContext,
        UserManager<ApplicationIdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<BootstrapAdminOptions> options)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options.Value;
    }

    /// <summary>
    /// Crea el esquema local necesario, los roles y el administrador inicial.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación del arranque.</param>
    /// <returns>Una tarea que representa la operación de seed.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureUserStatusColumnAsync(cancellationToken);

        await EnsureRoleAsync(AdminRole);
        await EnsureRoleAsync(UserRole);

        ApplicationIdentityUser? admin = await _userManager.FindByEmailAsync(_options.Email);
        if (admin is null)
        {
            admin = new ApplicationIdentityUser
            {
                UserName = _options.Email,
                Email = _options.Email,
                EmailConfirmed = true,
                Status = UserStatus.Active
            };

            IdentityResult creationResult = await _userManager.CreateAsync(admin, _options.Password);
            EnsureSuccess(creationResult, "The bootstrap administrator could not be created.");
        }

        if (!await _userManager.IsInRoleAsync(admin, AdminRole))
        {
            IdentityResult roleResult = await _userManager.AddToRoleAsync(admin, AdminRole);
            EnsureSuccess(roleResult, "The bootstrap administrator role could not be assigned.");
        }

    }

    /// <summary>
    /// Añade de forma idempotente el estado funcional a bases creadas por versiones anteriores.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación de la actualización.</param>
    /// <returns>Una tarea que representa la actualización del esquema.</returns>
    private async Task EnsureUserStatusColumnAsync(CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"Status\" integer NOT NULL DEFAULT 1;",
            cancellationToken);
    }

    /// <summary>
    /// Crea un rol si todavía no existe.
    /// </summary>
    /// <param name="roleName">Nombre del rol.</param>
    /// <returns>Una tarea que representa la operación.</returns>
    private async Task EnsureRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        IdentityResult result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        EnsureSuccess(result, $"The identity role '{roleName}' could not be created.");
    }

    /// <summary>
    /// Convierte un resultado fallido de Identity en un error de arranque seguro.
    /// </summary>
    /// <param name="result">Resultado de Identity.</param>
    /// <param name="message">Mensaje técnico seguro sin secretos.</param>
    private static void EnsureSuccess(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join(", ", result.Errors.Select(error => error.Code));
        throw new InvalidOperationException($"{message} Identity errors: {errors}");
    }
}
