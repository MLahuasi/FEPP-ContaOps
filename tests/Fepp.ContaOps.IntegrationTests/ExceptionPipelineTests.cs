using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
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
        ConcurrentBag<string> logs = [];
        await using TestApiFactory factory = new(logs);
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
        string traceId = document.RootElement.GetProperty("traceId").GetString()!;
        Assert.Single(logs, log => log.Contains("Unhandled exception", StringComparison.Ordinal));
        Assert.Contains(logs, log => log.Contains(traceId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Fábrica de host que agrega el controller exclusivo de esta prueba.
    /// </summary>
    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly ConcurrentBag<string> _logs;

        /// <summary>
        /// Inicializa la fábrica de pruebas con el almacenamiento de logs.
        /// </summary>
        /// <param name="logs">Almacenamiento donde se capturan mensajes estructurados.</param>
        public TestApiFactory(ConcurrentBag<string> logs)
        {
            _logs = logs;
        }

        /// <summary>
        /// Personaliza los servicios MVC para descubrir el controller de prueba.
        /// </summary>
        /// <param name="builder">Constructor del host de pruebas.</param>
        /// <returns>Constructor configurado con la aplicación de controllers de prueba.</returns>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(new TestLoggerProvider(_logs)));
            builder.ConfigureServices(services =>
            {
                services.AddControllers()
                    .AddApplicationPart(typeof(PipelineTestController).Assembly);
            });
        }
    }

    /// <summary>
    /// Proveedor de logging mínimo para comprobar correlación y cardinalidad.
    /// </summary>
    private sealed class TestLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentBag<string> _logs;

        /// <summary>
        /// Inicializa el proveedor de logging de pruebas.
        /// </summary>
        /// <param name="logs">Almacenamiento de mensajes.</param>
        public TestLoggerProvider(ConcurrentBag<string> logs)
        {
            _logs = logs;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_logs);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Logger de pruebas que conserva el texto formateado del evento.
    /// </summary>
    private sealed class TestLogger : ILogger
    {
        private readonly ConcurrentBag<string> _logs;

        /// <summary>
        /// Inicializa el logger de pruebas.
        /// </summary>
        /// <param name="logs">Almacenamiento de mensajes.</param>
        public TestLogger(ConcurrentBag<string> logs)
        {
            _logs = logs;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Information;
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _logs.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
