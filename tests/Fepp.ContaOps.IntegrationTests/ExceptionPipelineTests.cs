using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Fepp.ContaOps.IntegrationTests;

/// <summary>
/// Verifica el pipeline HTTP de manejo global de excepciones.
/// </summary>
public sealed class ExceptionPipelineTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine").Build();

    /// <summary>
    /// Inicia el contenedor PostgreSQL antes de ejecutar la prueba.
    /// </summary>
    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    /// <summary>
    /// Libera el contenedor PostgreSQL después de ejecutar la prueba.
    /// </summary>
    public Task DisposeAsync()
    {
        return _database.DisposeAsync().AsTask();
    }
    /// <summary>
    /// Comprueba que una excepción inesperada produce ProblemDetails seguro.
    /// </summary>
    [Fact]
    public async Task UnexpectedException_ReturnsSafeProblemDetails()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
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
        private readonly string _connectionString;

        /// <summary>
        /// Inicializa la fábrica de pruebas con el almacenamiento de logs.
        /// </summary>
        public TestApiFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Personaliza los servicios MVC para descubrir el controller de prueba.
        /// </summary>
        /// <param name="builder">Constructor del host de pruebas.</param>
        /// <returns>Constructor configurado con la aplicación de controllers de prueba.</returns>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", _connectionString);
            builder.UseSetting("Jwt:Issuer", "FEPP-ContaOps.Tests");
            builder.UseSetting("Jwt:Audience", "FEPP-ContaOps.Tests");
            builder.UseSetting("Jwt:SigningKey", "test-signing-key-with-at-least-32-bytes");
            builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
            builder.UseSetting("BootstrapAdmin:Email", "admin@example.com");
            builder.UseSetting("BootstrapAdmin:Password", "AdminPassword123!");
            builder.UseSetting("UserInvitation:TokenLifetimeHours", "24");
            builder.UseSetting("UserInvitation:ActivationBaseUrl", "https://client.test/activate");
            builder.UseSetting("Email:Smtp:Host", "localhost");
            builder.UseSetting("Email:Smtp:Port", "587");
            builder.UseSetting("Email:Smtp:SecurityMode", "StartTls");
            builder.UseSetting("Email:Smtp:Username", "test@example.com");
            builder.UseSetting("Email:Smtp:Password", "test-provider-secret");
            builder.UseSetting("Email:Smtp:FromAddress", "test@example.com");
            builder.UseSetting("Email:Smtp:TimeoutSeconds", "5");
            builder.ConfigureServices(services =>
            {
                services.AddControllers()
                    .AddApplicationPart(typeof(PipelineTestController).Assembly);
            });
        }
    }

}
