using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fepp.ContaOps.IntegrationTests;

/// <summary>
/// Verifica el pipeline HTTP de manejo global de excepciones.
/// </summary>
public sealed class ExceptionPipelineTests
{
    /// <summary>
    /// Comprueba que una excepción inesperada produce ProblemDetails seguro.
    /// </summary>
    [Fact]
    public async Task UnexpectedException_ReturnsSafeProblemDetails()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/test-pipeline/unexpected-error");
        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("unexpected_error", document.RootElement.GetProperty("code").GetString());
        Assert.True(document.RootElement.GetProperty("traceId").GetString() is not null);
        Assert.DoesNotContain("InternalOperationDetails", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Fábrica de host que agrega el controller exclusivo de esta prueba.
    /// </summary>
    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        /// <summary>
        /// Personaliza los servicios MVC para descubrir el controller de prueba.
        /// </summary>
        /// <param name="builder">Constructor del host de pruebas.</param>
        /// <returns>Constructor configurado con la aplicación de controllers de prueba.</returns>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddControllers()
                    .AddApplicationPart(typeof(PipelineTestController).Assembly);
            });
        }
    }
}
