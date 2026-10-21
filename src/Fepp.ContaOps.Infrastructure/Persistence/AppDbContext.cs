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
    /// Sesiones persistidas de los usuarios.
    /// </summary>
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    /// <summary>
    /// Refresh tokens persistidos mediante su hash criptográfico.
    /// </summary>
    public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();

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

        builder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(session => session.SessionId);

            entity.Property(session => session.SessionId)
                .HasColumnType("uuid");
            entity.Property(session => session.UserId)
                .HasColumnType("character varying(450)")
                .IsRequired();
            entity.Property(session => session.SecurityStamp)
                .HasColumnType("text")
                .IsRequired();
            entity.Property(session => session.Status)
                .HasColumnType("integer")
                .IsRequired();
            entity.Property(session => session.CreatedAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(session => session.ExpiresAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(session => session.RevokedAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(session => session.RevocationReason)
                .HasColumnType("character varying(200)");

            entity.HasOne<ApplicationIdentityUser>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(session => new { session.UserId, session.Status })
                .HasDatabaseName("IX_UserSessions_UserId_Status");
            entity.HasIndex(session => session.UserId)
                .HasDatabaseName("UX_UserSessions_ActiveUser")
                .IsUnique()
                .HasFilter("\"Status\" = 1");
        });

        builder.Entity<UserRefreshToken>(entity =>
        {
            entity.ToTable("UserRefreshTokens");
            entity.HasKey(token => token.TokenId);

            entity.Property(token => token.TokenId)
                .HasColumnType("uuid");
            entity.Property(token => token.SessionId)
                .HasColumnType("uuid")
                .IsRequired();
            entity.Property(token => token.TokenHash)
                .HasColumnType("bytea")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(token => token.CreatedAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(token => token.ExpiresAtUtc)
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(token => token.ConsumedAtUtc)
                .HasColumnType("timestamp with time zone");
            entity.Property(token => token.RevokedAtUtc)
                .HasColumnType("timestamp with time zone");

            entity.HasOne<UserSession>()
                .WithMany()
                .HasForeignKey(token => token.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(token => token.SessionId)
                .HasDatabaseName("UX_UserRefreshTokens_UsableSession")
                .IsUnique()
                .HasFilter("\"ConsumedAtUtc\" IS NULL AND \"RevokedAtUtc\" IS NULL");
            entity.HasIndex(token => token.ExpiresAtUtc)
                .HasDatabaseName("IX_UserRefreshTokens_ExpiresAtUtc");
        });
    }
}
