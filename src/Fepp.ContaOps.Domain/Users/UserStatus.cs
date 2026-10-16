namespace Fepp.ContaOps.Domain.Users;

/// <summary>
/// Estado funcional de una cuenta de usuario.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// La cuenta espera que el usuario establezca su primera contraseña.
    /// </summary>
    PendingActivation,

    /// <summary>
    /// La cuenta fue activada y puede autenticarse.
    /// </summary>
    Active
}
