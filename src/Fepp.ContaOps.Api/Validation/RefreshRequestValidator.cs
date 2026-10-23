using FluentValidation;
using Fepp.ContaOps.Api.Contracts.Requests;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Valida la forma del request de renovación.
/// </summary>
public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Inicializa las reglas de validación del refresh token.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}
