using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Proveedor de tokens de Identity que persiste únicamente hashes de tokens opacos.
/// </summary>
public sealed class HashedUserTokenProvider : IUserTwoFactorTokenProvider<ApplicationIdentityUser>
{
    /// <summary>
    /// Almacén persistente de tokens opacos.
    /// </summary>
    private readonly IOpaqueTokenStore _tokenStore;

    /// <summary>
    /// Operaciones criptográficas de los tokens.
    /// </summary>
    private readonly OpaqueTokenCryptography _cryptography;

    /// <summary>
    /// Configuración de vigencia de las invitaciones.
    /// </summary>
    private readonly UserInvitationOptions _invitationOptions;

    /// <summary>
    /// Inicializa el proveedor de tokens opacos.
    /// </summary>
    /// <param name="tokenStore">Almacén persistente de tokens.</param>
    /// <param name="cryptography">Operaciones criptográficas.</param>
    /// <param name="invitationOptions">Configuración de invitaciones.</param>
    public HashedUserTokenProvider(
        IOpaqueTokenStore tokenStore,
        OpaqueTokenCryptography cryptography,
        IOptions<UserInvitationOptions> invitationOptions)
    {
        _tokenStore = tokenStore;
        _cryptography = cryptography;
        _invitationOptions = invitationOptions.Value;
    }

    /// <inheritdoc />
    public Task<bool> CanGenerateTwoFactorTokenAsync(
        UserManager<ApplicationIdentityUser> manager,
        ApplicationIdentityUser user)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        string purpose,
        UserManager<ApplicationIdentityUser> manager,
        ApplicationIdentityUser user)
    {
        ArgumentException.ThrowIfNullOrEmpty(purpose);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);

        string securityStamp = await manager.GetSecurityStampAsync(user);
        DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAtUtc = createdAtUtc.AddHours(_invitationOptions.TokenLifetimeHours);

        return await _tokenStore.StoreAsync(
            user.Id,
            purpose,
            securityStamp,
            createdAtUtc,
            expiresAtUtc);
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(
        string purpose,
        string token,
        UserManager<ApplicationIdentityUser> manager,
        ApplicationIdentityUser user)
    {
        ArgumentException.ThrowIfNullOrEmpty(purpose);
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        OpaqueTokenRecord? persistedToken = await _tokenStore.FindAsync(user.Id, purpose);
        if (persistedToken is null)
        {
            return false;
        }

        string securityStamp = await manager.GetSecurityStampAsync(user);
        return _cryptography.Matches(token, persistedToken.TokenHash)
            && purpose.Equals(persistedToken.Purpose, StringComparison.Ordinal)
            && securityStamp.Length > 0
            && securityStamp.Equals(persistedToken.SecurityStamp, StringComparison.Ordinal)
            && DateTimeOffset.UtcNow < persistedToken.ExpiresAtUtc;
    }
}
