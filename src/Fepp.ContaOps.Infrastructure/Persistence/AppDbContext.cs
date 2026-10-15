using Fepp.ContaOps.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fepp.ContaOps.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core de la aplicación y ASP.NET Core Identity.
/// </summary>
public sealed class AppDbContext : IdentityDbContext<ApplicationIdentityUser, IdentityRole, string>
{
    /// <summary>
    /// Inicializa el contexto con las opciones del proveedor.
    /// </summary>
    /// <param name="options">Opciones de EF Core.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
