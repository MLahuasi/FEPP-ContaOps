using System.Data;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Domain.Users;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Almacena una única sesión activa por usuario y su refresh token inicial.
/// </summary>
public sealed class UserSessionStore : IUserSessionStore
{
    /// <summary>
    /// Motivo seguro de revocación aplicado por el endpoint de logout.
    /// </summary>
    private const string LogoutRevocationReason = "Logout";

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
    /// Crea la sesión de login y reemplaza cualquier sesión activa anterior.
    /// </summary>
    /// <param name="user">Usuario autenticado.</param>
    /// <param name="createdAtUtc">Instante UTC de creación.</param>
    /// <param name="expiresAtUtc">Instante UTC de expiración.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Identificador de la nueva sesión.</returns>
    async Task<UserSessionResult> IUserSessionStore.ReplaceActiveSessionAsync(
        User user,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        UserSessionCreationResult result = await ReplaceActiveSessionAsync(
            user.Id.Value,
            user.SecurityStamp,
            createdAtUtc,
            expiresAtUtc,
            expiresAtUtc,
            cancellationToken);
        return new UserSessionResult(result.SessionId, result.RefreshToken);
    }

    /// <summary>
    /// Consume atómicamente el refresh token de una sesión activa.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión del token.</param>
    /// <param name="refreshToken">Valor plano presentado por el cliente.</param>
    /// <param name="nowUtc">Instante UTC usado para evaluar la vigencia.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Resultado del intento de consumo.</returns>
    public async Task<RefreshTokenConsumptionStatus> ConsumeRefreshTokenAsync(
        Guid sessionId,
        string refreshToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredValue(refreshToken, nameof(refreshToken));

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            UserSession? session = await _dbContext.UserSessions
                .SingleOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);
            UserRefreshToken? persistedToken = await FindRefreshTokenAsync(
                sessionId,
                refreshToken,
                cancellationToken);
            ApplicationIdentityUser? user = session is null
                ? null
                : await _dbContext.Users
                    .SingleOrDefaultAsync(item => item.Id == session.UserId, cancellationToken);

            if (session is null || persistedToken is null
                || user is null
                || user.Status != UserStatus.Active
                || session.SecurityStamp != user.SecurityStamp
                || !_cryptography.Matches(refreshToken, persistedToken.TokenHash))
            {
                await transaction.CommitAsync(cancellationToken);
                return RefreshTokenConsumptionStatus.Invalid;
            }

            if (persistedToken.ConsumedAtUtc is not null || persistedToken.RevokedAtUtc is not null)
            {
                await RevokeSessionStateAsync(session, nowUtc, "RefreshTokenReuseDetected", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RefreshTokenConsumptionStatus.Reused;
            }

            if (session.Status != UserSessionStatus.Active
                || session.ExpiresAtUtc <= nowUtc
                || persistedToken.ExpiresAtUtc <= nowUtc)
            {
                MarkSessionExpired(session, nowUtc);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RefreshTokenConsumptionStatus.Expired;
            }

            persistedToken.ConsumedAtUtc = nowUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RefreshTokenConsumptionStatus.Consumed;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Consume el refresh token presentado y crea su reemplazo en una única transacción.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión del token.</param>
    /// <param name="refreshToken">Valor plano presentado por el cliente.</param>
    /// <param name="nowUtc">Instante UTC usado para evaluar la vigencia.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Estado de la operación y nuevo token cuando corresponde.</returns>
    public async Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(
        Guid sessionId,
        string refreshToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredValue(refreshToken, nameof(refreshToken));

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            UserSession? session = await _dbContext.UserSessions
                .SingleOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);
            UserRefreshToken? persistedToken = await FindRefreshTokenAsync(
                sessionId,
                refreshToken,
                cancellationToken);
            ApplicationIdentityUser? user = session is null
                ? null
                : await _dbContext.Users
                    .SingleOrDefaultAsync(item => item.Id == session.UserId, cancellationToken);

            if (session is null || persistedToken is null
                || user is null
                || user.Status != UserStatus.Active
                || session.SecurityStamp != user.SecurityStamp)
            {
                await transaction.CommitAsync(cancellationToken);
                return new RefreshTokenRotationResult(RefreshTokenConsumptionStatus.Invalid, null);
            }

            if (persistedToken.ConsumedAtUtc is not null || persistedToken.RevokedAtUtc is not null)
            {
                await RevokeSessionStateAsync(session, nowUtc, "RefreshTokenReuseDetected", cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new RefreshTokenRotationResult(RefreshTokenConsumptionStatus.Reused, null);
            }

            if (session.Status != UserSessionStatus.Active
                || session.ExpiresAtUtc <= nowUtc
                || persistedToken.ExpiresAtUtc <= nowUtc)
            {
                MarkSessionExpired(session, nowUtc);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new RefreshTokenRotationResult(RefreshTokenConsumptionStatus.Expired, null);
            }

            string replacement = _tokenGenerator.Generate();
            persistedToken.ConsumedAtUtc = nowUtc;
            _dbContext.UserRefreshTokens.Add(new UserRefreshToken
            {
                TokenId = Guid.NewGuid(),
                SessionId = sessionId,
                TokenHash = _cryptography.Hash(replacement),
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = session.ExpiresAtUtc
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RefreshTokenRotationResult(RefreshTokenConsumptionStatus.Consumed, replacement);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Resuelve la sesión por el hash del token y delega la rotación atómica.
    /// </summary>
    /// <param name="refreshToken">Valor plano presentado por el cliente.</param>
    /// <param name="nowUtc">Instante UTC usado para evaluar la vigencia.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de Application sin hashes ni entidades persistentes.</returns>
    async Task<Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationResult>
        IUserSessionStore.RotateRefreshTokenAsync(
            string refreshToken,
            DateTimeOffset nowUtc,
            CancellationToken cancellationToken)
    {
        ValidateRequiredValue(refreshToken, nameof(refreshToken));
        UserRefreshToken? persistedToken = await FindRefreshTokenByValueAsync(refreshToken, cancellationToken);
        if (persistedToken is null)
        {
            return new(
                Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus.Invalid,
                null,
                null,
                null);
        }

        UserSession? session = await _dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.SessionId == persistedToken.SessionId, cancellationToken);
        if (session is null)
        {
            return new(
                Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus.Invalid,
                null,
                null,
                null);
        }

        RefreshTokenRotationResult rotation = await RotateRefreshTokenAsync(
            session.SessionId,
            refreshToken,
            nowUtc,
            cancellationToken);
        Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus status = rotation.Status switch
        {
            RefreshTokenConsumptionStatus.Consumed => Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus.Rotated,
            RefreshTokenConsumptionStatus.Reused => Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus.Reused,
            RefreshTokenConsumptionStatus.Expired => Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus.Expired,
            _ => Fepp.ContaOps.Application.Abstractions.RefreshTokenRotationStatus.Invalid
        };
        return new(status, session.UserId, session.SessionId, rotation.RefreshToken);
    }

    /// <summary>
    /// Adapta la revocación del puerto de Application al almacén persistente.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión.</param>
    /// <param name="revokedAtUtc">Instante UTC de revocación.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la revocación.</returns>
    Task IUserSessionStore.RevokeSessionAsync(
        Guid sessionId,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        return RevokeSessionAsync(
            sessionId,
            LogoutRevocationReason,
            revokedAtUtc,
            cancellationToken);
    }

    /// <summary>
    /// Revoca una sesión y todos sus refresh tokens todavía utilizables.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión que se revoca.</param>
    /// <param name="reason">Motivo seguro de revocación.</param>
    /// <param name="revokedAtUtc">Instante UTC de la revocación.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Una tarea que representa la operación de persistencia.</returns>
    public async Task RevokeSessionAsync(
        Guid sessionId,
        string reason,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredValue(reason, nameof(reason));

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            UserSession? session = await _dbContext.UserSessions
                .SingleOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);
            if (session is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            await RevokeSessionStateAsync(session, revokedAtUtc, reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Marca como expiradas las sesiones activas cuyo plazo ya terminó.
    /// </summary>
    /// <param name="nowUtc">Instante UTC usado para evaluar la expiración.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Cantidad de sesiones marcadas como expiradas.</returns>
    public async Task<int> ExpireSessionsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            List<UserSession> sessions = await _dbContext.UserSessions
                .Where(session => session.Status == UserSessionStatus.Active
                    && session.ExpiresAtUtc <= nowUtc)
                .ToListAsync(cancellationToken);

            foreach (UserSession session in sessions)
            {
                MarkSessionExpired(session, nowUtc);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return sessions.Count;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Comprueba que una sesión JWT exista, pertenezca al usuario indicado y siga activa.
    /// </summary>
    /// <param name="sessionId">Identificador de sesión obtenido del claim sid.</param>
    /// <param name="userId">Identificador de usuario obtenido del claim sub.</param>
    /// <param name="nowUtc">Instante UTC utilizado para evaluar la vigencia.</param>
    /// <param name="cancellationToken">Token para cancelar la consulta.</param>
    /// <returns><see langword="true"/> cuando la sesión persistida es utilizable.</returns>
    public async Task<bool> IsSessionActiveAsync(
        Guid sessionId,
        string userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        UserSession? session = await _dbContext.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.SessionId == sessionId, cancellationToken);
        ApplicationIdentityUser? user = session is null
            ? null
            : await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == session.UserId, cancellationToken);

        return session is not null
            && session.UserId == userId
            && session.Status == UserSessionStatus.Active
            && session.ExpiresAtUtc > nowUtc
            && user is not null
            && user.Status == UserStatus.Active
            && session.SecurityStamp == user.SecurityStamp;
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
    /// Busca el refresh token por sesión y compara su hash sin conservar el valor plano.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión propietaria.</param>
    /// <param name="refreshToken">Valor plano presentado.</param>
    /// <param name="cancellationToken">Token de cancelación de la consulta.</param>
    /// <returns>Token persistido coincidente o <see langword="null"/>.</returns>
    private async Task<UserRefreshToken?> FindRefreshTokenAsync(
        Guid sessionId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        List<UserRefreshToken> persistedTokens = await _dbContext.UserRefreshTokens
            .Where(item => item.SessionId == sessionId)
            .ToListAsync(cancellationToken);
        return persistedTokens.SingleOrDefault(item =>
            _cryptography.Matches(refreshToken, item.TokenHash));
    }

    /// <summary>
    /// Busca un refresh token por su hash entre todas las sesiones.
    /// </summary>
    /// <param name="refreshToken">Valor plano presentado.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Token persistido coincidente o <see langword="null"/>.</returns>
    private async Task<UserRefreshToken?> FindRefreshTokenByValueAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        List<UserRefreshToken> persistedTokens = await _dbContext.UserRefreshTokens
            .ToListAsync(cancellationToken);
        return persistedTokens.SingleOrDefault(item =>
            _cryptography.Matches(refreshToken, item.TokenHash));
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

    /// <summary>
    /// Revoca la sesión y los refresh tokens no consumidos asociados.
    /// </summary>
    /// <param name="session">Sesión que se actualiza.</param>
    /// <param name="revokedAtUtc">Instante UTC de revocación.</param>
    /// <param name="reason">Motivo seguro de revocación.</param>
    /// <param name="cancellationToken">Token para cancelar la consulta.</param>
    private async Task RevokeSessionStateAsync(
        UserSession session,
        DateTimeOffset revokedAtUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        if (session.Status != UserSessionStatus.Revoked)
        {
            session.Status = UserSessionStatus.Revoked;
            session.RevokedAtUtc = revokedAtUtc;
            session.RevocationReason = reason;
        }

        List<UserRefreshToken> refreshTokens = await _dbContext.UserRefreshTokens
            .Where(token => token.SessionId == session.SessionId
                && token.ConsumedAtUtc == null
                && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (UserRefreshToken refreshToken in refreshTokens)
        {
            refreshToken.RevokedAtUtc = revokedAtUtc;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Marca una sesión activa como expirada.
    /// </summary>
    /// <param name="session">Sesión que se actualiza.</param>
    /// <param name="nowUtc">Instante UTC de evaluación.</param>
    private static void MarkSessionExpired(UserSession session, DateTimeOffset nowUtc)
    {
        if (session.Status == UserSessionStatus.Active && session.ExpiresAtUtc <= nowUtc)
        {
            session.Status = UserSessionStatus.Expired;
        }
    }
}
