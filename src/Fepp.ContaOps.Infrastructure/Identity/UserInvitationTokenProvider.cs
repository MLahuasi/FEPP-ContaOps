using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Proveedor Data Protection con vigencia exclusiva para invitaciones.
/// </summary>
public sealed class UserInvitationTokenProvider : DataProtectorTokenProvider<ApplicationIdentityUser>
{
    /// <summary>
    /// Inicializa el proveedor con las opciones de vigencia configuradas.
    /// </summary>
    /// <param name="dataProtectionProvider">Proveedor de protección de datos.</param>
    /// <param name="options">Opciones específicas del proveedor.</param>
    /// <param name="logger">Logger interno de Identity.</param>
    public UserInvitationTokenProvider(
        IDataProtectionProvider dataProtectionProvider,
        IOptions<UserInvitationTokenProviderOptions> options,
        ILogger<DataProtectorTokenProvider<ApplicationIdentityUser>> logger)
        : base(dataProtectionProvider, options, logger)
    {
    }
}
