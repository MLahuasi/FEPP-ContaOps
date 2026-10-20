using System.Security.Cryptography;
using Fepp.ContaOps.Application.Abstractions;
using Microsoft.AspNetCore.WebUtilities;

namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Genera tokens opacos mediante un generador criptográficamente seguro.
/// </summary>
public sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    /// <summary>
    /// Cantidad de bytes aleatorios de cada token opaco.
    /// </summary>
    public const int TokenByteLength = 32;

    /// <summary>
    /// Genera 32 bytes aleatorios y los codifica como Base64Url.
    /// </summary>
    /// <returns>Token opaco sin padding Base64Url.</returns>
    public string Generate()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return WebEncoders.Base64UrlEncode(tokenBytes);
    }
}
