using System.Security.Cryptography;
using System.Text;
using Fepp.ContaOps.Infrastructure.Persistence;

namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Proporciona las operaciones criptográficas y las validaciones de tokens opacos.
/// </summary>
public sealed class OpaqueTokenCryptography
{
    /// <summary>
    /// Cantidad de bytes producida por SHA-256.
    /// </summary>
    public const int HashByteLength = 32;

    /// <summary>
    /// Calcula el hash SHA-256 de un token opaco.
    /// </summary>
    /// <param name="token">Token público recibido.</param>
    /// <returns>Hash SHA-256 del token en bytes.</returns>
    public byte[] Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    /// <summary>
    /// Compara un token con su hash persistido en tiempo constante.
    /// </summary>
    /// <param name="token">Token público recibido.</param>
    /// <param name="expectedHash">Hash persistido esperado.</param>
    /// <returns><see langword="true"/> cuando el hash coincide.</returns>
    public bool Matches(string token, ReadOnlySpan<byte> expectedHash)
    {
        ArgumentNullException.ThrowIfNull(token);
        byte[] actualHash = Hash(token);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <summary>
    /// Valida el token, su propósito, el sello de seguridad y su vigencia temporal.
    /// </summary>
    /// <param name="token">Token público recibido.</param>
    /// <param name="persistedToken">Registro persistido del token.</param>
    /// <param name="expectedPurpose">Propósito requerido por la operación.</param>
    /// <param name="currentSecurityStamp">Sello vigente del usuario.</param>
    /// <param name="nowUtc">Instante UTC usado para evaluar la expiración.</param>
    /// <returns><see langword="true"/> cuando todas las validaciones son correctas.</returns>
    public bool IsValid(
        string token,
        HashedUserToken persistedToken,
        string expectedPurpose,
        string currentSecurityStamp,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(persistedToken);
        ArgumentNullException.ThrowIfNull(expectedPurpose);
        ArgumentNullException.ThrowIfNull(currentSecurityStamp);

        bool tokenMatches = Matches(token, persistedToken.TokenHash);

        return token.Length > 0
            && expectedPurpose.Length > 0
            && currentSecurityStamp.Length > 0
            && tokenMatches
            && expectedPurpose.Equals(persistedToken.Purpose, StringComparison.Ordinal)
            && currentSecurityStamp.Equals(persistedToken.SecurityStamp, StringComparison.Ordinal)
            && nowUtc < persistedToken.ExpiresAtUtc;
    }
}
