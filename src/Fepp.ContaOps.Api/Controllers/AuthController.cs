using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Api.Contracts.Requests;
using Fepp.ContaOps.Api.Contracts.Responses;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Application.Users.ActivateUser;
using Fepp.ContaOps.Application.Users.LoginUser;
using Fepp.ContaOps.Application.Users.RefreshUser;
using Fepp.ContaOps.Application.Users.LogoutUser;
using Fepp.ContaOps.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
    /// Caso de uso de renovación de sesión.
    /// </summary>
    private readonly RefreshUser _refreshUser;

    /// <summary>
    /// Caso de uso de cierre de sesión.
    /// </summary>
    private readonly LogoutUser _logoutUser;

    /// <summary>
    /// Caso de uso de activación de usuarios.
    /// </summary>
    private readonly ActivateUser _activateUser;

    /// <summary>
    /// Traductor de errores de Application a respuestas HTTP.
    /// </summary>
    private readonly ApplicationErrorProblemDetailsMapper _errorMapper;

    /// <summary>
    /// Inicializa el controlador de autenticación.
    /// </summary>
    /// <param name="loginUser">Caso de uso de inicio de sesión.</param>
    /// <param name="activateUser">Caso de uso de activación.</param>
    /// <param name="refreshUser">Caso de uso de renovación.</param>
    /// <param name="logoutUser">Caso de uso de cierre de sesión.</param>
    /// <param name="errorMapper">Traductor de errores esperados.</param>
    public AuthController(
        LoginUser loginUser,
        ActivateUser activateUser,
        RefreshUser refreshUser,
        LogoutUser logoutUser,
        ApplicationErrorProblemDetailsMapper errorMapper)
    {
        _loginUser = loginUser;
        _activateUser = activateUser;
        _refreshUser = refreshUser;
        _logoutUser = logoutUser;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Revoca persistentemente la sesión del Bearer actual.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Respuesta sin contenido cuando la sesión fue revocada.</returns>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        string? sessionValue = User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value
            ?? User.FindFirst(ClaimTypes.Sid)?.Value;
        if (!Guid.TryParse(sessionValue, out Guid sessionId))
        {
            return Unauthorized();
        }

        await _logoutUser.ExecuteAsync(sessionId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Renueva una sesión mediante rotación obligatoria del refresh token.
    /// </summary>
    /// <param name="request">Refresh token opaco.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Nuevos tokens o un error neutral de autenticación.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        Result<TokenResult> result = await _refreshUser.ExecuteAsync(
            request.RefreshToken,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return _errorMapper.Map(HttpContext, result.Error!);
        }

        TokenResult token = result.Value!;
        return Ok(new LoginResponse(token.AccessToken, "Bearer", token.ExpiresIn, token.RefreshToken));
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
        return Ok(new LoginResponse(token.AccessToken, "Bearer", token.ExpiresIn, token.RefreshToken));
    }

    /// <summary>
    /// Activa una cuenta pendiente mediante un token válido.
    /// </summary>
    /// <param name="request">Datos de activación de la cuenta.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    /// <returns>Una respuesta sin contenido cuando la cuenta fue activada.</returns>
    [AllowAnonymous]
    [HttpPost("activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Activate(
        ActivateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out Guid userId))
        {
            ModelState.AddModelError(nameof(request.UserId), "UserId must be a valid identifier.");
            return ProblemDetailsResponseFactory.CreateModelStateValidationResult(
                HttpContext,
                ModelState);
        }

        Result<User> result = await _activateUser.ExecuteAsync(
            new ActivateUserInput(
                new UserId(userId.ToString()),
                request.Token,
                request.NewPassword),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return _errorMapper.Map(HttpContext, result.Error!);
        }

        return NoContent();
    }
}
