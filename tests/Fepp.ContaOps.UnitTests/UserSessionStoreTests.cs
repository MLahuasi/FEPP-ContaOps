using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Persistence;
using Fepp.ContaOps.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica la persistencia transaccional de sesiones y refresh tokens.
/// </summary>
public sealed class UserSessionStoreTests : IAsyncLifetime
{
    /// <summary>
    /// Conexión abierta que mantiene viva la base SQLite en memoria.
    /// </summary>
    private SqliteConnection _connection = null!;

    /// <summary>
    /// Contexto de prueba con el modelo persistente de la aplicación.
    /// </summary>
    private AppDbContext _dbContext = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _dbContext.Users.Add(new ApplicationIdentityUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            SecurityStamp = "stamp-1"
        });
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    /// <summary>
    /// Comprueba que reemplazar una sesión deja solo una activa y revoca la anterior.
    /// </summary>
    [Fact]
    public async Task ReplaceActiveSession_LeavesOneActiveSessionAndHashesRefreshTokens()
    {
        UserSessionStore store = CreateStore();
        DateTimeOffset firstCreatedAt = UtcNow;
        UserSessionCreationResult first = await store.ReplaceActiveSessionAsync(
            "user-1",
            "stamp-1",
            firstCreatedAt,
            firstCreatedAt.AddDays(1),
            firstCreatedAt.AddHours(2));

        UserSessionCreationResult second = await store.ReplaceActiveSessionAsync(
            "user-1",
            "stamp-1",
            firstCreatedAt.AddMinutes(1),
            firstCreatedAt.AddDays(1),
            firstCreatedAt.AddHours(2));

        List<UserSession> sessions = await _dbContext.UserSessions.ToListAsync();
        List<UserRefreshToken> refreshTokens = await _dbContext.UserRefreshTokens.ToListAsync();

        Assert.Single(sessions, session => session.Status == UserSessionStatus.Active);
        Assert.Contains(sessions, session => session.SessionId == first.SessionId
            && session.Status == UserSessionStatus.Revoked);
        Assert.Contains(sessions, session => session.SessionId == second.SessionId);
        Assert.All(refreshTokens, token => Assert.Equal(32, token.TokenHash.Length));
        Assert.DoesNotContain(refreshTokens, token =>
            Convert.ToBase64String(token.TokenHash) == first.RefreshToken);
    }

    /// <summary>
    /// Comprueba que el índice parcial impide dos sesiones activas del mismo usuario.
    /// </summary>
    [Fact]
    public async Task Database_RejectsTwoActiveSessionsForOneUser()
    {
        DateTimeOffset nowUtc = UtcNow;
        _dbContext.UserSessions.AddRange(
            new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = "user-1",
                SecurityStamp = "stamp-1",
                Status = UserSessionStatus.Active,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddHours(1)
            },
            new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = "user-1",
                SecurityStamp = "stamp-1",
                Status = UserSessionStatus.Active,
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = nowUtc.AddHours(1)
            });

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
    }

    /// <summary>
    /// Comprueba el consumo único y la revocación por reutilización.
    /// </summary>
    [Fact]
    public async Task ConsumeRefreshToken_RejectsReuseAndRevokesSession()
    {
        UserSessionStore store = CreateStore();
        DateTimeOffset nowUtc = UtcNow;
        UserSessionCreationResult created = await store.ReplaceActiveSessionAsync(
            "user-1",
            "stamp-1",
            nowUtc,
            nowUtc.AddDays(1),
            nowUtc.AddHours(2));

        RefreshTokenConsumptionStatus consumed = await store.ConsumeRefreshTokenAsync(
            created.SessionId,
            created.RefreshToken,
            nowUtc.AddMinutes(1));
        RefreshTokenConsumptionStatus reused = await store.ConsumeRefreshTokenAsync(
            created.SessionId,
            created.RefreshToken,
            nowUtc.AddMinutes(2));

        UserSession session = await _dbContext.UserSessions
            .SingleAsync(item => item.SessionId == created.SessionId);
        UserRefreshToken refreshToken = await _dbContext.UserRefreshTokens
            .SingleAsync(item => item.SessionId == created.SessionId);

        Assert.Equal(RefreshTokenConsumptionStatus.Consumed, consumed);
        Assert.Equal(RefreshTokenConsumptionStatus.Reused, reused);
        Assert.Equal(UserSessionStatus.Revoked, session.Status);
        Assert.Equal("RefreshTokenReuseDetected", session.RevocationReason);
        Assert.NotNull(refreshToken.ConsumedAtUtc);
    }

    /// <summary>
    /// Comprueba que consumir un token de una sesión vencida marca la sesión como expirada.
    /// </summary>
    [Fact]
    public async Task ConsumeRefreshToken_WhenSessionExpired_MarksSessionExpired()
    {
        UserSessionStore store = CreateStore();
        DateTimeOffset createdAtUtc = UtcNow;
        UserSessionCreationResult created = await store.ReplaceActiveSessionAsync(
            "user-1",
            "stamp-1",
            createdAtUtc,
            createdAtUtc.AddMinutes(1),
            createdAtUtc.AddHours(1));

        RefreshTokenConsumptionStatus result = await store.ConsumeRefreshTokenAsync(
            created.SessionId,
            created.RefreshToken,
            createdAtUtc.AddMinutes(1));
        UserSession session = await _dbContext.UserSessions
            .SingleAsync(item => item.SessionId == created.SessionId);

        Assert.Equal(RefreshTokenConsumptionStatus.Expired, result);
        Assert.Equal(UserSessionStatus.Expired, session.Status);
    }

    /// <summary>
    /// Comprueba que un cambio del sello de seguridad invalida el refresh token.
    /// </summary>
    [Fact]
    public async Task ConsumeRefreshToken_RejectsChangedSecurityStamp()
    {
        UserSessionStore store = CreateStore();
        DateTimeOffset nowUtc = UtcNow;
        UserSessionCreationResult created = await store.ReplaceActiveSessionAsync(
            "user-1",
            "stamp-1",
            nowUtc,
            nowUtc.AddDays(1),
            nowUtc.AddHours(2));

        ApplicationIdentityUser user = await _dbContext.Users.SingleAsync(item => item.Id == "user-1");
        user.SecurityStamp = "stamp-2";
        await _dbContext.SaveChangesAsync();

        RefreshTokenConsumptionStatus result = await store.ConsumeRefreshTokenAsync(
            created.SessionId,
            created.RefreshToken,
            nowUtc.AddMinutes(1));

        Assert.Equal(RefreshTokenConsumptionStatus.Invalid, result);
    }

    /// <summary>
    /// Comprueba que la validación Bearer rechaza estados de sesión y stamps inválidos.
    /// </summary>
    [Fact]
    public async Task IsSessionActive_RejectsRevokedExpiredAndChangedStamp()
    {
        UserSessionStore store = CreateStore();
        DateTimeOffset nowUtc = UtcNow;
        UserSession revokedSession = CreateSession(UserSessionStatus.Revoked, "stamp-1", nowUtc.AddHours(1));
        UserSession expiredSession = CreateSession(UserSessionStatus.Expired, "stamp-1", nowUtc.AddMinutes(-1));
        UserSession changedStampSession = CreateSession(UserSessionStatus.Active, "stamp-previous", nowUtc.AddHours(1));
        _dbContext.UserSessions.AddRange(revokedSession, expiredSession, changedStampSession);
        await _dbContext.SaveChangesAsync();

        Assert.False(await store.IsSessionActiveAsync(Guid.NewGuid(), "user-1", nowUtc));
        Assert.False(await store.IsSessionActiveAsync(revokedSession.SessionId, "user-1", nowUtc));
        Assert.False(await store.IsSessionActiveAsync(expiredSession.SessionId, "user-1", nowUtc));
        Assert.False(await store.IsSessionActiveAsync(changedStampSession.SessionId, "user-1", nowUtc));
    }

    /// <summary>
    /// Crea el almacén con las dependencias criptográficas de prueba.
    /// </summary>
    /// <returns>Almacén de sesiones configurado con el contexto de prueba.</returns>
    private UserSessionStore CreateStore()
    {
        return new UserSessionStore(
            _dbContext,
            new OpaqueTokenGenerator(),
            new OpaqueTokenCryptography());
    }

    /// <summary>
    /// Crea una sesión de prueba con el estado y stamp indicados.
    /// </summary>
    /// <param name="status">Estado persistente de la sesión.</param>
    /// <param name="securityStamp">Sello almacenado en la sesión.</param>
    /// <param name="expiresAtUtc">Expiración persistida.</param>
    /// <returns>Sesión no persistida.</returns>
    private static UserSession CreateSession(
        UserSessionStatus status,
        string securityStamp,
        DateTimeOffset expiresAtUtc)
    {
        return new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = "user-1",
            SecurityStamp = securityStamp,
            Status = status,
            CreatedAtUtc = UtcNow.AddMinutes(-1),
            ExpiresAtUtc = expiresAtUtc
        };
    }

    /// <summary>
    /// Obtiene un instante fijo para que las pruebas sean deterministas.
    /// </summary>
    /// <value>Instante UTC de referencia.</value>
    private static DateTimeOffset UtcNow =>
        new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
}
