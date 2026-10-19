using FluentValidation.Results;
using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Api.Exceptions;
using Fepp.ContaOps.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica la traducción del contrato de errores a ProblemDetails.
/// </summary>
public sealed class ProblemDetailsResponseFactoryTests
{
    /// <summary>
    /// Comprueba que los errores de validación incluyen campos y traceId.
    /// </summary>
    [Fact]
    public void CreateValidationResult_ReturnsUniformProblemDetails()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        ValidationResult validationResult = new(
        [
            new ValidationFailure("Email", "Email is required."),
            new ValidationFailure("Email", "Email is invalid.")
        ]);

        BadRequestObjectResult result = ProblemDetailsResponseFactory.CreateValidationResult(
            httpContext,
            validationResult);
        ProblemDetails problemDetails = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("validation_error", problemDetails.Extensions["code"]);
        Assert.Equal("trace-id", problemDetails.Extensions["traceId"]);
        Assert.Contains("errors", problemDetails.Extensions.Keys);
    }

    /// <summary>
    /// Comprueba que un recurso inexistente se traduce a HTTP 404.
    /// </summary>
    [Fact]
    public void CreateApplicationErrorResult_NotFound_Returns404()
    {
        ObjectResult result = ProblemDetailsResponseFactory.CreateApplicationErrorResult(
            CreateHttpContext(),
            new ResourceNotFoundError("account", "account-1"));
        ProblemDetails problemDetails = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal("resource_not_found", problemDetails.Extensions["code"]);
        Assert.DoesNotContain("account-1", problemDetails.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Comprueba que un token de activación inválido se traduce a 400 estable.
    /// </summary>
    [Fact]
    public void CreateApplicationErrorResult_InvalidActivationToken_Returns400()
    {
        ObjectResult result = ProblemDetailsResponseFactory.CreateApplicationErrorResult(
            CreateHttpContext(),
            InvalidActivationTokenError.Instance);
        ProblemDetails problemDetails = Assert.IsType<ProblemDetails>(result.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("invalid_activation_token", problemDetails.Extensions["code"]);
    }

    /// <summary>
    /// Comprueba que un fallo de correo se traduce a 503 seguro.
    /// </summary>
    [Fact]
    public async Task EmailDeliveryUnavailable_Returns503ProblemDetails()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        GlobalExceptionHandler handler = new(NullLogger<GlobalExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(
            httpContext,
            new EmailDeliveryUnavailableException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);
    }

    /// <summary>
    /// Comprueba que una cancelación del cliente no se convierte en HTTP 500.
    /// </summary>
    [Fact]
    public async Task CancelledRequest_Returns499()
    {
        DefaultHttpContext httpContext = CreateHttpContext();
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        httpContext.RequestAborted = cancellationTokenSource.Token;
        GlobalExceptionHandler handler = new(NullLogger<GlobalExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(
            httpContext,
            new OperationCanceledException(),
            httpContext.RequestAborted);

        Assert.True(handled);
        Assert.Equal(499, httpContext.Response.StatusCode);
    }

    /// <summary>
    /// Crea un contexto HTTP mínimo con un identificador de correlación estable.
    /// </summary>
    /// <returns>Contexto HTTP utilizado por las pruebas.</returns>
    private static DefaultHttpContext CreateHttpContext()
    {
        DefaultHttpContext httpContext = new();
        httpContext.TraceIdentifier = "trace-id";
        httpContext.Request.Path = "/test";
        return httpContext;
    }
}
