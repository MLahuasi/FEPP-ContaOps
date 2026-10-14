using Fepp.ContaOps.Api.Validation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fepp.ContaOps.Api.Exceptions;

/// <summary>
/// Convierte excepciones no controladas en respuestas ProblemDetails seguras.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Inicializa el handler global de excepciones.
    /// </summary>
    /// <param name="logger">Logger estructurado del handler.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Maneja una excepción y escribe una respuesta segura para el cliente.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud fallida.</param>
    /// <param name="exception">Excepción capturada por el middleware.</param>
    /// <param name="cancellationToken">Token para cancelar la escritura de la respuesta.</param>
    /// <returns>Siempre devuelve <see langword="true"/> cuando la respuesta fue generada.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "HTTP request cancelled. TraceId: {TraceId}, Method: {Method}, Path: {Path}",
                httpContext.TraceIdentifier,
                httpContext.Request.Method,
                httpContext.Request.Path);

            await WriteProblemDetailsAsync(
                httpContext,
                499,
                "Request cancelled.",
                "The request was cancelled by the client.",
                "request_cancelled",
                cancellationToken);
            return true;
        }

        _logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}, Method: {Method}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Method,
            httpContext.Request.Path);

        await WriteProblemDetailsAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "The request could not be completed.",
            "unexpected_error",
            cancellationToken);
        return true;
    }

    /// <summary>
    /// Escribe un ProblemDetails sin incluir información de la excepción interna.
    /// </summary>
    /// <param name="httpContext">Contexto HTTP de la solicitud.</param>
    /// <param name="status">Código HTTP de la respuesta.</param>
    /// <param name="title">Título seguro de la respuesta.</param>
    /// <param name="detail">Detalle seguro de la respuesta.</param>
    /// <param name="code">Código estable del error.</param>
    /// <param name="cancellationToken">Token para cancelar la escritura.</param>
    /// <returns>Una tarea que representa la escritura de la respuesta.</returns>
    private static async Task WriteProblemDetailsAsync(
        HttpContext httpContext,
        int status,
        string title,
        string detail,
        string code,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            ProblemDetailsResponseFactory.Create(httpContext, status, title, detail, code),
            cancellationToken);
    }
}
