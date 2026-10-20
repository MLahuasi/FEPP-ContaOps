namespace Fepp.ContaOps.Infrastructure.Persistence;

/// <summary>
/// Token opaco persistido únicamente mediante su hash criptográfico.
/// </summary>
public sealed class HashedUserToken
{
    /// <summary>
    /// Identificador interno del token persistido.
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// Identificador del usuario propietario del token.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Propósito funcional del token.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Hash SHA-256 del token opaco.
    /// </summary>
    public byte[] TokenHash { get; set; } = [];

    /// <summary>
    /// Sello de seguridad vigente del usuario al crear el token.
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC de creación del token.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora UTC de expiración del token.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
