using FluentValidation;
using Fepp.ContaOps.Api.Contracts.Requests;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Valida la forma y los límites de transporte para activar una cuenta.
/// </summary>
public sealed class ActivateUserRequestValidator : AbstractValidator<ActivateUserRequest>
{
    /// <summary>
    /// Inicializa las reglas de validación del request.
    /// </summary>
    public ActivateUserRequestValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty()
            .MaximumLength(36)
            .Must(BeAValidUserId)
            .WithMessage("UserId must be a valid identifier.");

        RuleFor(request => request.Token)
            .NotEmpty()
            .MaximumLength(4096);

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MaximumLength(256);
    }

    /// <summary>
    /// Comprueba el formato de identificador utilizado por ASP.NET Core Identity.
    /// </summary>
    /// <param name="userId">Identificador recibido por HTTP.</param>
    /// <returns><see langword="true"/> cuando el valor representa un GUID válido.</returns>
    private static bool BeAValidUserId(string userId)
    {
        return Guid.TryParse(userId, out _);
    }
}
