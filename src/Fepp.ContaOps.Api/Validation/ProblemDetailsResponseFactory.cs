using FluentValidation.Results;
using Fepp.ContaOps.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Construye respuestas ProblemDetails con el contrato uniforme de la API.
/// </summary>
public static class ProblemDetailsResponseFactory
{
    /// <summary>
    /// Construye una respuesta HTTP para un error tipado de Application.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud actual.</param>
    /// <param name="error">Error esperado producido por Application.</param>
    /// <returns>Resultado HTTP con el estado correspondiente al error.</returns>
    public static ObjectResult CreateApplicationErrorResult(
        HttpContext httpContext,
        ApplicationError error)
    {
        int status = error switch
        {
            ValidationError => StatusCodes.Status400BadRequest,
            AuthenticationError => StatusCodes.Status401Unauthorized,
            ResourceNotFoundError => StatusCodes.Status404NotFound,
            ConflictError => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        string detail = error switch
        {
            ValidationError => "One or more validation errors occurred.",
            AuthenticationError => "The supplied credentials are invalid.",
            ResourceNotFoundError notFound => notFound.Message,
            ConflictError conflict => conflict.Message,
            _ => "The request could not be completed."
        };

        ProblemDetails problemDetails = Create(
            httpContext,
            status,
            status switch
            {
                StatusCodes.Status400BadRequest => "Request validation failed.",
                StatusCodes.Status401Unauthorized => "Authentication failed.",
                StatusCodes.Status404NotFound => "Resource not found.",
                StatusCodes.Status409Conflict => "Resource conflict.",
                _ => "An unexpected error occurred."
            },
            detail,
            error.Code);

        if (error is ValidationError validationError)
        {
            problemDetails.Extensions["errors"] = validationError.Errors;
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    /// <summary>
    /// Construye una respuesta de validación para los errores del request.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud actual.</param>
    /// <param name="validationResult">Resultado de FluentValidation.</param>
    /// <returns>Resultado HTTP con los errores agrupados por campo.</returns>
    public static BadRequestObjectResult CreateValidationResult(
        HttpContext httpContext,
        ValidationResult validationResult)
    {
        Dictionary<string, string[]> errors = validationResult.Errors
            .GroupBy(error => error.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        ProblemDetails problemDetails = Create(
            httpContext,
            StatusCodes.Status400BadRequest,
            "Request validation failed.",
            "One or more request fields are invalid.",
            "validation_error");
        problemDetails.Extensions["errors"] = errors;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    /// <summary>
    /// Construye un ProblemDetails con los datos comunes del contrato de errores.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud actual.</param>
    /// <param name="status">Código HTTP de la respuesta.</param>
    /// <param name="title">Título seguro del error.</param>
    /// <param name="detail">Detalle seguro del error.</param>
    /// <param name="code">Código estable del error.</param>
    /// <returns>ProblemDetails preparado para serialización.</returns>
    public static ProblemDetails Create(
        HttpContext httpContext,
        int status,
        string title,
        string detail,
        string code)
    {
        ProblemDetails problemDetails = new()
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["code"] = code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problemDetails;
    }
}
