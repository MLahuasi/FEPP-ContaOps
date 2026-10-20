using System.Data;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Almacena una única sesión activa por usuario y su refresh token inicial.
/// </summary>
public sealed class UserSessionStore
{
    /// <summary>
    /// Motivo seguro aplicado a la sesión anterior cuando se reemplaza.
    /// </summary>
    private const string ReplacedSessionReason = "ReplacedByNewSession";

    /// <summary>
    /// Contexto de persistencia de la aplicación.
    /// </summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Generador de tokens opacos criptográficamente seguros.
    /// </summary>
    private readonly IOpaqueTokenGenerator _tokenGenerator;

    /// <summary>
    /// Operaciones de hash y comparación de tokens.
    /// </summary>
    private readonly OpaqueTokenCryptography _cryptography;

    /// <summary>
    /// Inicializa el almacenamiento de sesiones.
    /// </summary>
    /// <param name="dbContext">Contexto de persistencia.</param>
    /// <param name="tokenGenerator">Generador de refresh tokens.</param>
    /// <param name="cryptography">Operaciones criptográficas de tokens.</param>
    public UserSessionStore(
        AppDbContext dbContext,
        IOpaqueTokenGenerator tokenGenerator,
        OpaqueTokenCryptography cryptography)
    {
        _dbContext = dbContext;
        _tokenGenerator = tokenGenerator;
        _cryptography = cryptography;
    }

    /// <summary>
    /// Reemplaza la sesión activa del usuario y crea su refresh token inicial.
    /// </summary>
    /// <param name="userId">Identificador del usuario propietario.</param>
    /// <param name="securityStamp">Sello de seguridad vigente del usuario.</param>
    /// <param name="createdAtUtc">Instante UTC de creación.</param>
    /// <param name="sessionExpiresAtUtc">Instante UTC de expiración de la sesión.</param>
    /// <param name="refreshTokenExpiresAtUtc">Instante UTC de expiración del refresh token.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Identificador de sesión y refresh token plano para entrega al cliente.</returns>
    public async Task<UserSessionCreationResult> ReplaceActiveSessionAsync(
        string userId,
        string securityStamp,
        DateTimeOffset createdAtUtc,
        DateTimeOffset sessionExpiresAtUtc,
        DateTimeOffset refreshTokenExpiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredValue(userId, nameof(userId));
        ValidateRequiredValue(securityStamp, nameof(securityStamp));
        ValidateExpiration(createdAtUtc, sessionExpiresAtUtc, nameof(sessionExpiresAtUtc));
        ValidateExpiration(createdAtUtc, refreshTokenExpiresAtUtc, nameof(refreshTokenExpiresAtUtc));

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            DateTimeOffset revokedAtUtc = createdAtUtc;
            List<UserSession> activeSessions = await _dbContext.UserSessions
                .Where(session => session.UserId == userId
                    && session.Status == UserSessionStatus.Active)
                .ToListAsync(cancellationToken);

            Guid[] replacedSessionIds = [.. activeSessions.Select(session => session.SessionId)];
            if (replacedSessionIds.Length > 0)
            {
                List<UserRefreshToken> previousRefreshTokens = await _dbContext.UserRefreshTokens
                    .Where(token => replacedSessionIds.Contains(token.SessionId)
                        && token.ConsumedAtUtc == null
                        && token.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);

                foreach (UserRefreshToken previousRefreshToken in previousRefreshTokens)
                {
                    previousRefreshToken.RevokedAtUtc = revokedAtUtc;
                }
            }

            foreach (UserSession activeSession in activeSessions)
            {
                activeSession.Status = UserSessionStatus.Revoked;
                activeSession.RevokedAtUtc = revokedAtUtc;
                activeSession.RevocationReason = ReplacedSessionReason;
            }

            string refreshToken = _tokenGenerator.Generate();
            UserSession session = new()
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                SecurityStamp = securityStamp,
                Status = UserSessionStatus.Active,
                CreatedAtUtc = createdAtUtc,
                ExpiresAtUtc = sessionExpiresAtUtc
            };
            UserRefreshToken newRefreshToken = new()
            {
                TokenId = Guid.NewGuid(),
                SessionId = session.SessionId,
                TokenHash = _cryptography.Hash(refreshToken),
                CreatedAtUtc = createdAtUtc,
                ExpiresAtUtc = refreshTokenExpiresAtUtc
            };

            _dbContext.UserSessions.Add(session);
            _dbContext.UserRefreshTokens.Add(newRefreshToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new UserSessionCreationResult(session.SessionId, refreshToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Valida un texto requerido.
    /// </summary>
    /// <param name="value">Valor que se valida.</param>
    /// <param name="parameterName">Nombre del parámetro.</param>
    private static void ValidateRequiredValue(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
    }

    /// <summary>
    /// Valida que una fecha de expiración sea posterior a la creación.
    /// </summary>
    /// <param name="createdAtUtc">Instante UTC de creación.</param>
    /// <param name="expiresAtUtc">Instante UTC de expiración.</param>
    /// <param name="parameterName">Nombre del parámetro de expiración.</param>
    private static void ValidateExpiration(
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        string parameterName)
    {
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "The expiration must be after creation.",
                parameterName);
        }
    }
}
