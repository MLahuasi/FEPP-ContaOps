using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Users.RefreshUser;

/// <summary>
/// Rota un refresh token y emite un nuevo token de acceso.
/// </summary>
public sealed class RefreshUser
{
    /// <summary>
    /// Almacén que valida y rota refresh tokens.
    /// </summary>
    private readonly IUserSessionStore _sessionStore;

    /// <summary>
    /// Repositorio que recupera el usuario asociado a la sesión.
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Emisor de tokens de acceso.
    /// </summary>
    private readonly ITokenIssuer _tokenIssuer;

    /// <summary>
    /// Inicializa el caso de uso de renovación de sesión.
    /// </summary>
    /// <param name="sessionStore">Almacén de sesiones y refresh tokens.</param>
    /// <param name="userRepository">Repositorio de usuarios.</param>
    /// <param name="tokenIssuer">Emisor de JWT.</param>
    public RefreshUser(
        IUserSessionStore sessionStore,
        IUserRepository userRepository,
        ITokenIssuer tokenIssuer)
    {
        _sessionStore = sessionStore;
        _userRepository = userRepository;
        _tokenIssuer = tokenIssuer;
    }

    /// <summary>
    /// Valida y rota el refresh token presentado.
    /// </summary>
    /// <param name="refreshToken">Token opaco presentado por el cliente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Tokens renovados o un error neutral de autenticación.</returns>
    public async Task<Result<TokenResult>> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<TokenResult>.Failure(AuthenticationError.Instance);
        }

        RefreshTokenRotationResult rotation = await _sessionStore.RotateRefreshTokenAsync(
            refreshToken,
            DateTimeOffset.UtcNow,
            cancellationToken);
        if (rotation.Status != RefreshTokenRotationStatus.Rotated
            || rotation.UserId is null
            || rotation.SessionId is null
            || rotation.RefreshToken is null)
        {
            return Result<TokenResult>.Failure(AuthenticationError.Instance);
        }

        User? user = await _userRepository.FindByIdAsync(rotation.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return Result<TokenResult>.Failure(AuthenticationError.Instance);
        }

        TokenResult token = await _tokenIssuer.IssueAsync(
            user,
            rotation.SessionId.Value,
            Guid.NewGuid().ToString("N"),
            cancellationToken);
        return Result<TokenResult>.Success(token with { RefreshToken = rotation.RefreshToken });
    }
}
