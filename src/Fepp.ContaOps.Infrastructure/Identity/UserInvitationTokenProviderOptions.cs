using Microsoft.AspNetCore.Identity;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Opciones del proveedor criptográfico dedicado a invitaciones.
/// </summary>
public sealed class UserInvitationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    /// <summary>
    /// Nombre estable del protector de tokens de invitación.
    /// </summary>
    public UserInvitationTokenProviderOptions()
    {
        Name = "UserInvitationDataProtectorTokenProvider";
    }
}
