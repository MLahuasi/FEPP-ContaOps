using FluentValidation.Results;
using Fepp.ContaOps.Api.Validation;
using Fepp.ContaOps.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
