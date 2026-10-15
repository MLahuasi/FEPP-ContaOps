using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Users.LoginUser;

/// <summary>
/// Coordina la autenticación de un usuario y la emisión de su token de acceso.
/// </summary>
public sealed class LoginUser
{
    /// <summary>
    /// Repositorio utilizado para localizar el usuario por correo.
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Servicio utilizado para comprobar las credenciales.
    /// </summary>
    private readonly IUserCredentialService _credentialService;

    /// <summary>
    /// Emisor utilizado para generar el token de acceso.
    /// </summary>
    private readonly ITokenIssuer _tokenIssuer;

    /// <summary>
    /// Inicializa el caso de uso de inicio de sesión.
    /// </summary>
    /// <param name="userRepository">Repositorio de usuarios autenticables.</param>
    /// <param name="credentialService">Servicio de comprobación de credenciales.</param>
    /// <param name="tokenIssuer">Emisor de tokens de acceso.</param>
    public LoginUser(
        IUserRepository userRepository,
        IUserCredentialService credentialService,
        ITokenIssuer tokenIssuer)
    {
        _userRepository = userRepository;
        _credentialService = credentialService;
        _tokenIssuer = tokenIssuer;
    }

    /// <summary>
    /// Autentica las credenciales y emite un token cuando son válidas.
    /// </summary>
    /// <param name="input">Datos de credenciales proporcionados.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>Token emitido o un error de credenciales inválidas.</returns>
    public async Task<Result<TokenResult>> ExecuteAsync(
        LoginUserInput input,
        CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.FindByEmailAsync(input.Email, cancellationToken);
        if (user is null)
        {
            return Result<TokenResult>.Failure(AuthenticationError.Instance);
        }

        bool passwordIsValid = await _credentialService.VerifyPasswordAsync(
            user,
            input.Password,
            cancellationToken);
        if (!passwordIsValid)
        {
            return Result<TokenResult>.Failure(AuthenticationError.Instance);
        }

        TokenResult token = await _tokenIssuer.IssueAsync(user, cancellationToken);
        return Result<TokenResult>.Success(token);
    }
}
