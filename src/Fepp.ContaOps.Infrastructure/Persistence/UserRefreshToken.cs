namespace Fepp.ContaOps.Infrastructure.Persistence;

/// <summary>
/// Refresh token persistido sin conservar el valor secreto original.
/// </summary>
public sealed class UserRefreshToken
{
    /// <summary>
    /// Identificador interno del refresh token persistido.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Identificador de la sesión a la que pertenece el token.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Hash SHA-256 del refresh token opaco.
    /// </summary>
    public byte[] TokenHash { get; set; } = [];

    /// <summary>
    /// Fecha y hora UTC de creación del refresh token.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora UTC de expiración del refresh token.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora UTC de consumo, si el token ya fue utilizado.
    /// </summary>
    public DateTimeOffset? ConsumedAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora UTC de revocación, si el token fue revocado.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }
}
