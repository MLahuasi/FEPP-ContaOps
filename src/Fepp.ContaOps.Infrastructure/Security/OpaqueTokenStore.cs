using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fepp.ContaOps.Infrastructure.Security;

/// <summary>
/// Implementación EF Core del almacenamiento de tokens opacos.
/// </summary>
public sealed class OpaqueTokenStore : IOpaqueTokenStore
{
    /// <summary>
    /// Contexto EF Core usado para persistir los tokens.
    /// </summary>
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Generador de tokens públicos.
    /// </summary>
    private readonly IOpaqueTokenGenerator _tokenGenerator;

    /// <summary>
    /// Operaciones criptográficas de los tokens.
    /// </summary>
    private readonly OpaqueTokenCryptography _cryptography;

    /// <summary>
    /// Inicializa el almacén de tokens opacos.
    /// </summary>
    /// <param name="dbContext">Contexto de persistencia.</param>
    /// <param name="tokenGenerator">Generador de tokens públicos.</param>
    /// <param name="cryptography">Operaciones criptográficas.</param>
    public OpaqueTokenStore(
        AppDbContext dbContext,
        IOpaqueTokenGenerator tokenGenerator,
        OpaqueTokenCryptography cryptography)
    {
        _dbContext = dbContext;
        _tokenGenerator = tokenGenerator;
        _cryptography = cryptography;
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(
        string userId,
        string purpose,
        string securityStamp,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredValue(userId, nameof(userId));
        ValidateRequiredValue(purpose, nameof(purpose));
        ValidateRequiredValue(securityStamp, nameof(securityStamp));
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "The token expiration must be after its creation.",
                nameof(expiresAtUtc));
        }

        string publicToken = _tokenGenerator.Generate();
        HashedUserToken? persistedToken = await _dbContext.UserSecurityTokens
            .SingleOrDefaultAsync(
                token => token.UserId == userId && token.Purpose == purpose,
                cancellationToken);

        if (persistedToken is null)
        {
            persistedToken = new HashedUserToken
            {
                TokenId = Guid.NewGuid(),
                UserId = userId,
                Purpose = purpose
            };
            _dbContext.UserSecurityTokens.Add(persistedToken);
        }

        persistedToken.TokenHash = _cryptography.Hash(publicToken);
        persistedToken.SecurityStamp = securityStamp;
        persistedToken.CreatedAtUtc = createdAtUtc;
        persistedToken.ExpiresAtUtc = expiresAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return publicToken;
    }

    /// <inheritdoc />
    public async Task<OpaqueTokenRecord?> FindAsync(
        string userId,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredValue(userId, nameof(userId));
        ValidateRequiredValue(purpose, nameof(purpose));

        HashedUserToken? persistedToken = await _dbContext.UserSecurityTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.UserId == userId && token.Purpose == purpose,
                cancellationToken);

        return persistedToken is null
            ? null
            : new OpaqueTokenRecord(
                persistedToken.TokenId,
                persistedToken.UserId,
                persistedToken.Purpose,
                [.. persistedToken.TokenHash],
                persistedToken.SecurityStamp,
                persistedToken.CreatedAtUtc,
                persistedToken.ExpiresAtUtc);
    }

    /// <inheritdoc />
    public Task<int> DeleteExpiredAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserSecurityTokens
            .Where(token => token.ExpiresAtUtc <= nowUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Valida un argumento de texto requerido.
    /// </summary>
    /// <param name="value">Valor a validar.</param>
    /// <param name="parameterName">Nombre del parámetro.</param>
    private static void ValidateRequiredValue(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
    }
}
