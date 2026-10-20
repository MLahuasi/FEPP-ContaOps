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
    /// Tokens opacos almacenados mediante su hash criptográfico.
    /// </summary>
    public DbSet<HashedUserToken> UserSecurityTokens => Set<HashedUserToken>();

    /// <summary>
    /// Inicializa el contexto con las opciones del proveedor.
    /// </summary>
    /// <param name="options">Opciones de EF Core.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configura el modelo persistente de Identity y de la aplicación.
    /// </summary>
    /// <param name="builder">Constructor del modelo de EF Core.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<HashedUserToken>(entity =>
        {
            entity.ToTable("UserSecurityTokens");
            entity.HasKey(token => token.TokenId);

            entity.Property(token => token.TokenId)
                .HasColumnType("uuid");
            entity.Property(token => token.UserId)
                .HasColumnType("character varying(450)")
                .IsRequired();
            entity.Property(token => token.Purpose)
                .HasColumnType("text")
                .IsRequired();
            entity.Property(token => token.TokenHash)
                .HasColumnType("bytea")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(token => token.SecurityStamp)
                .HasColumnType("text")
                .IsRequired();
            entity.Property(token => token.CreatedAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(token => token.ExpiresAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasOne<ApplicationIdentityUser>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(token => new { token.UserId, token.Purpose })
                .IsUnique();
        });
    }
}
