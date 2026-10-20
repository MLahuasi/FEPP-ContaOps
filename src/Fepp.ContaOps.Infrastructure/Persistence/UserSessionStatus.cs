namespace Fepp.ContaOps.Infrastructure.Persistence;

/// <summary>
/// Estado persistente de una sesión de usuario.
/// </summary>
public enum UserSessionStatus
{
    /// <summary>
    /// La sesión puede utilizarse mientras no haya expirado.
    /// </summary>
    Active = 1,

    /// <summary>
    /// La sesión fue revocada antes de su expiración.
    /// </summary>
    Revoked = 2,

    /// <summary>
    /// La sesión superó su fecha de expiración.
    /// </summary>
    Expired = 3
}
