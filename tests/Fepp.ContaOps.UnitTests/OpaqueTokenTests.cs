using Fepp.ContaOps.Infrastructure.Persistence;
using Fepp.ContaOps.Infrastructure.Security;
using Microsoft.AspNetCore.WebUtilities;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica la generación y validación criptográfica de tokens opacos.
/// </summary>
public sealed class OpaqueTokenTests
{
    /// <summary>
    /// Comprueba que un token contiene 32 bytes y usa Base64Url.
    /// </summary>
    [Fact]
    public void Generate_Returns32RandomBytesAsBase64Url()
    {
        OpaqueTokenGenerator generator = new();

        string token = generator.Generate();
        byte[] decodedToken = WebEncoders.Base64UrlDecode(token);

        Assert.Equal(OpaqueTokenGenerator.TokenByteLength, decodedToken.Length);
        Assert.DoesNotContain('=', token);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
    }

    /// <summary>
    /// Comprueba que dos generaciones consecutivas no producen el mismo token.
    /// </summary>
    [Fact]
    public void Generate_ProducesDifferentTokens()
    {
        OpaqueTokenGenerator generator = new();

        string firstToken = generator.Generate();
        string secondToken = generator.Generate();

        Assert.NotEqual(firstToken, secondToken);
    }

    /// <summary>
    /// Comprueba que el hash tiene el tamaño de SHA-256 y no contiene el token original.
    /// </summary>
    [Fact]
    public void Hash_IsFixedLengthAndDoesNotStoreOriginalToken()
    {
        OpaqueTokenCryptography cryptography = new();
        const string token = "opaque-token-value";

        byte[] hash = cryptography.Hash(token);

        Assert.Equal(OpaqueTokenCryptography.HashByteLength, hash.Length);
        Assert.NotEqual(token, Convert.ToBase64String(hash));
        Assert.True(cryptography.Matches(token, hash));
    }

    /// <summary>
    /// Comprueba que un token diferente no coincide con el hash persistido.
    /// </summary>
    [Fact]
    public void Matches_RejectsDifferentToken()
    {
        OpaqueTokenCryptography cryptography = new();
        byte[] hash = cryptography.Hash("expected-token");

        Assert.False(cryptography.Matches("different-token", hash));
    }

    /// <summary>
    /// Comprueba que un token vigente con propósito y sello correctos es válido.
    /// </summary>
    [Fact]
    public void IsValid_AcceptsMatchingPurposeStampAndExpiration()
    {
        OpaqueTokenCryptography cryptography = new();
        DateTimeOffset nowUtc = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        HashedUserToken persistedToken = CreatePersistedToken(cryptography, nowUtc.AddMinutes(1));

        bool isValid = cryptography.IsValid(
            "expected-token",
            persistedToken,
            "invitation",
            "stamp-1",
            nowUtc);

        Assert.True(isValid);
    }

    /// <summary>
    /// Comprueba que un propósito diferente invalida el token.
    /// </summary>
    [Fact]
    public void IsValid_RejectsDifferentPurpose()
    {
        OpaqueTokenCryptography cryptography = new();
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        HashedUserToken persistedToken = CreatePersistedToken(cryptography, nowUtc.AddMinutes(1));

        bool isValid = cryptography.IsValid(
            "expected-token",
            persistedToken,
            "password-reset",
            "stamp-1",
            nowUtc);

        Assert.False(isValid);
    }

    /// <summary>
    /// Comprueba que un sello de seguridad diferente invalida el token.
    /// </summary>
    [Fact]
    public void IsValid_RejectsDifferentSecurityStamp()
    {
        OpaqueTokenCryptography cryptography = new();
        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        HashedUserToken persistedToken = CreatePersistedToken(cryptography, nowUtc.AddMinutes(1));

        bool isValid = cryptography.IsValid(
            "expected-token",
            persistedToken,
            "invitation",
            "stamp-2",
            nowUtc);

        Assert.False(isValid);
    }

    /// <summary>
    /// Comprueba que un token vencido, incluido el instante límite, es inválido.
    /// </summary>
    [Fact]
    public void IsValid_RejectsExpiredToken()
    {
        OpaqueTokenCryptography cryptography = new();
        DateTimeOffset nowUtc = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        HashedUserToken persistedToken = CreatePersistedToken(cryptography, nowUtc);

        bool isValid = cryptography.IsValid(
            "expected-token",
            persistedToken,
            "invitation",
            "stamp-1",
            nowUtc);

        Assert.False(isValid);
    }

    /// <summary>
    /// Crea un registro persistido para las pruebas de validación.
    /// </summary>
    /// <param name="cryptography">Operaciones criptográficas de prueba.</param>
    /// <param name="expiresAtUtc">Fecha de expiración del registro.</param>
    /// <returns>Registro persistido con hash del token esperado.</returns>
    private static HashedUserToken CreatePersistedToken(
        OpaqueTokenCryptography cryptography,
        DateTimeOffset expiresAtUtc)
    {
        return new HashedUserToken
        {
            TokenId = Guid.NewGuid(),
            UserId = "user-1",
            Purpose = "invitation",
            TokenHash = cryptography.Hash("expected-token"),
            SecurityStamp = "stamp-1",
            CreatedAtUtc = expiresAtUtc.AddMinutes(-5),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
