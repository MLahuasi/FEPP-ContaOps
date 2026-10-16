using Microsoft.AspNetCore.Identity;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Usuario técnico persistido por ASP.NET Core Identity.
/// </summary>
public sealed class ApplicationIdentityUser : IdentityUser
{
    /// <summary>
    /// Estado funcional de la cuenta, independiente del estado técnico de Identity.
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;
}
