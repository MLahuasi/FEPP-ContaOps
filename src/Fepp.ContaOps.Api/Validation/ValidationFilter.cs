using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Ejecuta el validador FluentValidation asociado a cada argumento de acción.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Inicializa el filtro de validación.
    /// </summary>
    /// <param name="serviceProvider">Proveedor utilizado para resolver validadores registrados.</param>
    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Valida los argumentos de la acción antes de continuar hacia el controlador.
    /// </summary>
    /// <param name="context">Contexto de ejecución de la acción.</param>
    /// <param name="next">Siguiente filtro de la cadena de ejecución.</param>
    /// <returns>Una tarea que representa la ejecución del filtro.</returns>
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (object? argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            Type validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            ValidationResult validationResult = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            if (!validationResult.IsValid)
            {
                context.Result = ProblemDetailsResponseFactory.CreateValidationResult(
                    context.HttpContext,
                    validationResult);
                return;
            }
        }

        await next();
    }
}
