namespace Fepp.ContaOps.Infrastructure.Persistence;

/// <summary>
/// Sesión persistida de un usuario autenticado.
/// </summary>
public sealed class UserSession
{
    /// <summary>
    /// Identificador interno de la sesión persistida.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Identificador del usuario propietario de la sesión.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Sello de seguridad del usuario al crear la sesión.
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// Estado persistente de la sesión.
    /// </summary>
    public UserSessionStatus Status { get; set; } = UserSessionStatus.Active;

    /// <summary>
    /// Fecha y hora UTC de creación de la sesión.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora UTC de expiración de la sesión.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Fecha y hora UTC de revocación, si la sesión fue revocada.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Motivo seguro de revocación, sin incluir secretos ni tokens.
    /// </summary>
    public string? RevocationReason { get; set; }
}
