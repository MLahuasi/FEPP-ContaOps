using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Construye respuestas ProblemDetails con el contrato uniforme de la API.
/// </summary>
public static class ProblemDetailsResponseFactory
{
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
