using Fepp.ContaOps.Application.Abstractions;

namespace Fepp.ContaOps.Application.Users.LogoutUser;

/// <summary>
/// Revoca la sesión asociada al token de acceso actual.
/// </summary>
public sealed class LogoutUser
{
    /// <summary>
    /// Almacén persistente de sesiones.
    /// </summary>
    private readonly IUserSessionStore _sessionStore;

    /// <summary>
    /// Inicializa el caso de uso de logout.
    /// </summary>
    /// <param name="sessionStore">Almacén de sesiones persistidas.</param>
    public LogoutUser(IUserSessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    /// <summary>
    /// Revoca la sesión indicada por el claim del JWT autenticado.
    /// </summary>
    /// <param name="sessionId">Identificador de la sesión autenticada.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Una tarea que representa la operación.</returns>
    public Task ExecuteAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return _sessionStore.RevokeSessionAsync(
            sessionId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }
}
