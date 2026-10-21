namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Resultado interno del intento de consumo de un refresh token.
/// </summary>
public enum RefreshTokenConsumptionStatus
{
    /// <summary>
    /// El refresh token fue consumido correctamente.
    /// </summary>
    Consumed = 1,

    /// <summary>
    /// El refresh token no existe o no coincide con la sesión.
    /// </summary>
    Invalid = 2,

    /// <summary>
    /// El refresh token ya fue consumido o revocado.
    /// </summary>
    Reused = 3,

    /// <summary>
    /// El refresh token o la sesión ya expiraron.
    /// </summary>
    Expired = 4
}
