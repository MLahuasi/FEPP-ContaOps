using Fepp.ContaOps.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fepp.ContaOps.Api.Validation;

/// <summary>
/// Registra y traduce errores esperados de Application a respuestas HTTP.
/// </summary>
public sealed class ApplicationErrorProblemDetailsMapper
{
    private readonly ILogger<ApplicationErrorProblemDetailsMapper> _logger;

    /// <summary>
    /// Inicializa el traductor de errores de Application.
    /// </summary>
    /// <param name="logger">Logger estructurado para errores esperados.</param>
    public ApplicationErrorProblemDetailsMapper(
        ILogger<ApplicationErrorProblemDetailsMapper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registra y convierte un error esperado a una respuesta ProblemDetails.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud actual.</param>
    /// <param name="error">Error esperado producido por Application.</param>
    /// <returns>Respuesta HTTP con el contrato uniforme de errores.</returns>
    public ObjectResult Map(HttpContext httpContext, ApplicationError error)
    {
        _logger.LogWarning(
            "Expected application error. Code: {Code}, TraceId: {TraceId}",
            error.Code,
            httpContext.TraceIdentifier);

        return ProblemDetailsResponseFactory.CreateApplicationErrorResult(httpContext, error);
    }
}
