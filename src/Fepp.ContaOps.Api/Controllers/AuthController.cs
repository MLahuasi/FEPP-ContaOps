using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Api.Contracts.Requests;
using Fepp.ContaOps.Api.Contracts.Responses;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Application.Users.LoginUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fepp.ContaOps.Api.Controllers;

/// <summary>
/// Expone las operaciones de autenticación de usuarios.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Caso de uso de inicio de sesión.
    /// </summary>
    private readonly LoginUser _loginUser;

    /// <summary>
    /// Traductor de errores de Application a respuestas HTTP.
    /// </summary>
    private readonly ApplicationErrorProblemDetailsMapper _errorMapper;

    /// <summary>
    /// Inicializa el controlador de autenticación.
    /// </summary>
    /// <param name="loginUser">Caso de uso de inicio de sesión.</param>
    /// <param name="errorMapper">Traductor de errores esperados.</param>
    public AuthController(
        LoginUser loginUser,
        ApplicationErrorProblemDetailsMapper errorMapper)
    {
        _loginUser = loginUser;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Autentica un usuario y devuelve un token Bearer.
    /// </summary>
    /// <param name="request">Credenciales del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    /// <returns>Token de acceso o una respuesta de error uniforme.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<TokenResult> result = await _loginUser.ExecuteAsync(
            new LoginUserInput(request.Email, request.Password),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return _errorMapper.Map(HttpContext, result.Error!);
        }

        TokenResult token = result.Value!;
        return Ok(new LoginResponse(token.AccessToken, "Bearer", token.ExpiresIn));
    }
}
