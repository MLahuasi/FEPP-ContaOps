using FluentValidation;
using Fepp.ContaOps.Api.Contracts.Requests;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Valida los datos HTTP para crear una invitación de usuario.
/// </summary>
public sealed class CreateUserInvitationRequestValidator : AbstractValidator<CreateUserInvitationRequest>
{
    /// <summary>
    /// Inicializa las reglas de validación del request.
    /// </summary>
    public CreateUserInvitationRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}
