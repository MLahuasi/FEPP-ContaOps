using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Fepp.ContaOps.IntegrationTests;

/// <summary>
/// Verifica el flujo HTTP de login usando PostgreSQL efímero.
/// </summary>
public sealed class AuthIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine").Build();

    /// <summary>
    /// Inicia el contenedor PostgreSQL antes de ejecutar las pruebas.
    /// </summary>
    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    /// <summary>
    /// Libera el contenedor PostgreSQL después de ejecutar las pruebas.
    /// </summary>
    public Task DisposeAsync()
    {
        return _database.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Comprueba que el seed permite autenticarse con el administrador inicial.
    /// </summary>
    [Fact]
    public async Task LoginWithBootstrapAdmin_ReturnsBearerToken()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "admin@example.com", Password = "AdminPassword123!" });
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal("Bearer", document.RootElement.GetProperty("tokenType").GetString());
        string accessToken = document.RootElement.GetProperty("accessToken").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.True(document.RootElement.GetProperty("expiresIn").GetInt64() > 0);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        Assert.Equal("FEPP-ContaOps.Tests", jwt.Issuer);
        Assert.Contains("FEPP-ContaOps.Tests", jwt.Audiences);
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    /// <summary>
    /// Comprueba que credenciales inválidas devuelven una respuesta segura y uniforme.
    /// </summary>
    [Fact]
    public async Task LoginWithInvalidCredentials_ReturnsUnauthorizedProblemDetails()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "admin@example.com", Password = "wrong-password" });
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal("invalid_credentials", document.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }

    /// <summary>
    /// Fábrica que proporciona secretos de prueba y la conexión del contenedor.
    /// </summary>
    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Inicializa la fábrica con la conexión PostgreSQL efímera.
        /// </summary>
        /// <param name="connectionString">Cadena de conexión del contenedor.</param>
        public TestApiFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Default", _connectionString);
            builder.UseSetting("Jwt:Issuer", "FEPP-ContaOps.Tests");
            builder.UseSetting("Jwt:Audience", "FEPP-ContaOps.Tests");
            builder.UseSetting("Jwt:SigningKey", "test-signing-key-with-at-least-32-bytes");
            builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "15");
            builder.UseSetting("BootstrapAdmin:Email", "admin@example.com");
            builder.UseSetting("BootstrapAdmin:Password", "AdminPassword123!");
        }
    }
}
