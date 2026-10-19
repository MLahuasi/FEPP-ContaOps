using Fepp.ContaOps.Api.Contracts.Requests;
using Fepp.ContaOps.Api.Contracts.Responses;
using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Application.Users.CreateUserInvitation;
using Fepp.ContaOps.Application.Users.ResendUserInvitation;
using Fepp.ContaOps.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fepp.ContaOps.Api.Controllers;

/// <summary>
/// Expone las operaciones administrativas del ciclo de invitaciones.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class UserInvitationsController : ControllerBase
{
    /// <summary>
    /// Caso de uso de creación de invitaciones.
    /// </summary>
    private readonly CreateUserInvitation _createUserInvitation;

    /// <summary>
    /// Caso de uso de reenvío de invitaciones.
    /// </summary>
    private readonly ResendUserInvitation _resendUserInvitation;

    /// <summary>
    /// Traductor de errores de Application a respuestas HTTP.
    /// </summary>
    private readonly ApplicationErrorProblemDetailsMapper _errorMapper;

    /// <summary>
    /// Inicializa el controlador de invitaciones.
    /// </summary>
    /// <param name="createUserInvitation">Caso de uso de creación.</param>
    /// <param name="resendUserInvitation">Caso de uso de reenvío.</param>
    /// <param name="errorMapper">Traductor de errores esperados.</param>
    public UserInvitationsController(
        CreateUserInvitation createUserInvitation,
        ResendUserInvitation resendUserInvitation,
        ApplicationErrorProblemDetailsMapper errorMapper)
    {
        _createUserInvitation = createUserInvitation;
        _resendUserInvitation = resendUserInvitation;
        _errorMapper = errorMapper;
    }

    /// <summary>
    /// Crea una cuenta pendiente y solicita el envío de su invitación.
    /// </summary>
    /// <param name="request">Datos de la cuenta que se desea invitar.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    /// <returns>La representación pública de la cuenta creada.</returns>
    [HttpPost("invitations")]
    [ProducesResponseType(typeof(UserInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateInvitation(
        CreateUserInvitationRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserInvitationResult> result = await _createUserInvitation.ExecuteAsync(
            new CreateUserInvitationInput(request.Email),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return _errorMapper.Map(HttpContext, result.Error!);
        }

        User user = result.Value!.User;
        UserInvitationResponse response = ToResponse(user);
        return Created($"/api/users/{user.Id.Value}", response);
    }

    /// <summary>
    /// Genera y solicita el envío de una nueva invitación para una cuenta pendiente.
    /// </summary>
    /// <param name="id">Identificador de la cuenta pendiente.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    /// <returns>Una respuesta sin contenido cuando la invitación fue reenviada.</returns>
    [HttpPost("{id}/invitations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ResendInvitation(
        string id,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out Guid userId))
        {
            ModelState.AddModelError(nameof(id), "UserId must be a valid identifier.");
            return ProblemDetailsResponseFactory.CreateModelStateValidationResult(
                HttpContext,
                ModelState);
        }

        Result<UserInvitationResult> result = await _resendUserInvitation.ExecuteAsync(
            new ResendUserInvitationInput(new UserId(userId.ToString())),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return _errorMapper.Map(HttpContext, result.Error!);
        }

        return NoContent();
    }

    /// <summary>
    /// Convierte el usuario de Application al response público de invitación.
    /// </summary>
    /// <param name="user">Usuario creado por el caso de uso.</param>
    /// <returns>Response sin token ni datos técnicos.</returns>
    private static UserInvitationResponse ToResponse(User user)
    {
        string role = user.Roles.FirstOrDefault() ?? string.Empty;
        return new UserInvitationResponse(user.Id.Value, user.Email, role);
    }
}
