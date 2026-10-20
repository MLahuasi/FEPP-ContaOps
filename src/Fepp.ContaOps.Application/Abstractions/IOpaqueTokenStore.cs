namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para almacenar y consultar tokens opacos por usuario y propósito.
/// </summary>
public interface IOpaqueTokenStore
{
    /// <summary>
    /// Crea o reemplaza el token activo del usuario para un propósito.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="purpose">Propósito funcional del token.</param>
    /// <param name="securityStamp">Sello de seguridad vigente.</param>
    /// <param name="createdAtUtc">Fecha y hora UTC de creación.</param>
    /// <param name="expiresAtUtc">Fecha y hora UTC de expiración.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Token público generado.</returns>
    Task<string> StoreAsync(
        string userId,
        string purpose,
        string securityStamp,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta el token persistido para un usuario y propósito.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="purpose">Propósito funcional del token.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Datos persistidos o <see langword="null"/> si no existen.</returns>
    Task<OpaqueTokenRecord?> FindAsync(
        string userId,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consume atómicamente el token si coincide con su hash, sello y vigencia.
    /// </summary>
    /// <param name="userId">Identificador del usuario propietario.</param>
    /// <param name="purpose">Propósito funcional del token.</param>
    /// <param name="token">Token público recibido.</param>
    /// <param name="securityStamp">Sello de seguridad vigente del usuario.</param>
    /// <param name="nowUtc">Instante UTC usado para evaluar la expiración.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns><see langword="true"/> si se eliminó exactamente el token válido.</returns>
    Task<bool> ConsumeAsync(
        string userId,
        string purpose,
        string token,
        string securityStamp,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina tokens cuya fecha de expiración ya llegó.
    /// </summary>
    /// <param name="nowUtc">Instante UTC usado para evaluar la expiración.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Cantidad de filas eliminadas.</returns>
    Task<int> DeleteExpiredAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Datos de un token opaco recuperado desde persistencia.
/// </summary>
/// <param name="TokenId">Identificador interno del token.</param>
/// <param name="UserId">Identificador del usuario.</param>
/// <param name="Purpose">Propósito funcional del token.</param>
/// <param name="TokenHash">Hash SHA-256 persistido.</param>
/// <param name="SecurityStamp">Sello de seguridad usado al crearlo.</param>
/// <param name="CreatedAtUtc">Fecha y hora UTC de creación.</param>
/// <param name="ExpiresAtUtc">Fecha y hora UTC de expiración.</param>
public sealed record OpaqueTokenRecord(
    Guid TokenId,
    string UserId,
    string Purpose,
    byte[] TokenHash,
    string SecurityStamp,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
