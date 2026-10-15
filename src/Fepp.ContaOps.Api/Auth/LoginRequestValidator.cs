using FluentValidation;

namespace Fepp.ContaOps.Api.Auth;

/// <summary>
/// Valida los datos HTTP del inicio de sesión.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Inicializa las reglas de validación del request.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
