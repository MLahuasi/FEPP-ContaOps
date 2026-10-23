using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Persistence;

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
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == ClaimTypes.Email);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == ClaimTypes.Role);
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
    /// Comprueba que el segundo login revoca el token emitido para el primer equipo.
    /// </summary>
    [Fact]
    public async Task SuccessiveLogins_LeaveOnlyTheLatestBearerSessionUsable()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient firstDevice = factory.CreateClient();
        using HttpClient secondDevice = factory.CreateClient();

        string firstToken = await LoginAndReadTokenAsync(firstDevice);
        string secondToken = await LoginAndReadTokenAsync(secondDevice);
        JwtSecurityToken firstJwt = new JwtSecurityTokenHandler().ReadJwtToken(firstToken);
        JwtSecurityToken secondJwt = new JwtSecurityTokenHandler().ReadJwtToken(secondToken);

        string firstSessionId = firstJwt.Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value;
        string secondSessionId = secondJwt.Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value;
        string firstJti = firstJwt.Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;
        string secondJti = secondJwt.Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value;

        Assert.NotEqual(firstSessionId, secondSessionId);
        Assert.NotEqual(firstJti, secondJti);

        firstDevice.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", firstToken);
        secondDevice.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondToken);

        HttpResponseMessage firstProtectedResponse = await firstDevice.PostAsJsonAsync(
            "/api/users/invitations",
            new { Email = "first-device@example.com" });
        HttpResponseMessage secondProtectedResponse = await secondDevice.PostAsJsonAsync(
            "/api/users/invitations",
            new { Email = "second-device@example.com" });

        Assert.Equal(HttpStatusCode.Unauthorized, firstProtectedResponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, secondProtectedResponse.StatusCode);
    }

    /// <summary>
    /// Comprueba en PostgreSQL que un cambio de stamp y la expiración revocan el JWT.
    /// </summary>
    [Fact]
    public async Task SecurityStampChangeAndSessionExpiration_RejectBearerToken()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient client = factory.CreateClient();

        string stampToken = await LoginAndReadTokenAsync(client);
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            UserManager<ApplicationIdentityUser> userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationIdentityUser>>();
            ApplicationIdentityUser user = (await userManager.FindByEmailAsync("admin@example.com"))!;
            IdentityResult result = await userManager.UpdateSecurityStampAsync(user);
            Assert.True(result.Succeeded);
        }

        HttpResponseMessage stampResponse = await SendProtectedAsync(client, stampToken);
        Assert.Equal(HttpStatusCode.Unauthorized, stampResponse.StatusCode);

        string expiringToken = await LoginAndReadTokenAsync(client);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(expiringToken);
        Guid sessionId = Guid.Parse(jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value);
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            UserSession session = await dbContext.UserSessions.SingleAsync(item => item.SessionId == sessionId);
            session.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        HttpResponseMessage expirationResponse = await SendProtectedAsync(client, expiringToken);
        Assert.Equal(HttpStatusCode.Unauthorized, expirationResponse.StatusCode);
    }

    /// <summary>
    /// Comprueba que renovar consume el refresh anterior y que reutilizarlo revoca la sesión.
    /// </summary>
    [Fact]
    public async Task RefreshRotation_RejectsReuseAndInvalidatesTheReplacement()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient client = factory.CreateClient();
        (string accessToken, string refreshToken) = await LoginAndReadTokensAsync(client);

        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        string refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        using JsonDocument refreshDocument = JsonDocument.Parse(refreshBody);
        string replacement = refreshDocument.RootElement.GetProperty("refreshToken").GetString()!;
        string replacementAccessToken = refreshDocument.RootElement.GetProperty("accessToken").GetString()!;
        Assert.NotEqual(refreshToken, replacement);

        HttpResponseMessage reusedResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reusedResponse.StatusCode);

        HttpResponseMessage replacementAccessResponse = await SendProtectedAsync(client, replacementAccessToken);
        HttpResponseMessage originalAccessResponse = await SendProtectedAsync(client, accessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replacementAccessResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, originalAccessResponse.StatusCode);
    }

    /// <summary>
    /// Comprueba que dos rotaciones concurrentes no producen dos refresh utilizables.
    /// </summary>
    [Fact]
    public async Task ConcurrentRefreshes_ProduceAtMostOneSuccessfulRotation()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient firstClient = factory.CreateClient();
        using HttpClient secondClient = factory.CreateClient();
        (_, string refreshToken) = await LoginAndReadTokensAsync(firstClient);

        Task<HttpResponseMessage> firstRefresh = firstClient.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        Task<HttpResponseMessage> secondRefresh = secondClient.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        HttpResponseMessage[] responses = await Task.WhenAll(firstRefresh, secondRefresh);

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Comprueba que un refresh de sesión expirada devuelve un error neutral.
    /// </summary>
    [Fact]
    public async Task ExpiredSession_RejectsRefreshToken()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient client = factory.CreateClient();
        (string accessToken, string refreshToken) = await LoginAndReadTokensAsync(client);
        Guid sessionId = Guid.Parse(new JwtSecurityTokenHandler()
            .ReadJwtToken(accessToken)
            .Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid)
            .Value);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            UserSession session = await dbContext.UserSessions.SingleAsync(item => item.SessionId == sessionId);
            session.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Comprueba que logout invalida el Bearer actual y el refresh persistido.
    /// </summary>
    [Fact]
    public async Task Logout_ImmediatelyInvalidatesAccessAndRefreshTokens()
    {
        await using TestApiFactory factory = new(_database.GetConnectionString());
        using HttpClient client = factory.CreateClient();
        (string accessToken, string refreshToken) = await LoginAndReadTokensAsync(client);

        HttpResponseMessage logoutResponse = await SendLogoutAsync(client, accessToken);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        HttpResponseMessage accessResponse = await SendProtectedAsync(client, accessToken);
        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, accessResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    /// <summary>
    /// Inicia sesión con el administrador de pruebas y devuelve el JWT emitido.
    /// </summary>
    /// <param name="client">Cliente HTTP que representa un equipo.</param>
    /// <returns>JWT Bearer emitido por la API.</returns>
    private static async Task<string> LoginAndReadTokenAsync(HttpClient client)
    {
        (string accessToken, _) = await LoginAndReadTokensAsync(client);
        return accessToken;
    }

    /// <summary>
    /// Inicia sesión y devuelve ambos tokens de la respuesta.
    /// </summary>
    /// <param name="client">Cliente HTTP que representa un equipo.</param>
    /// <returns>JWT de acceso y refresh token opaco.</returns>
    private static async Task<(string AccessToken, string RefreshToken)> LoginAndReadTokensAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "admin@example.com", Password = "AdminPassword123!" });
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            document.RootElement.GetProperty("accessToken").GetString()!,
            document.RootElement.GetProperty("refreshToken").GetString()!);
    }

    /// <summary>
    /// Ejecuta una solicitud administrativa autenticada para probar el pipeline Bearer.
    /// </summary>
    /// <param name="client">Cliente HTTP de pruebas.</param>
    /// <param name="token">JWT Bearer presentado.</param>
    /// <returns>Respuesta HTTP de la API.</returns>
    private static async Task<HttpResponseMessage> SendProtectedAsync(HttpClient client, string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/users/invitations")
        {
            Content = JsonContent.Create(new { Email = $"probe-{Guid.NewGuid():N}@example.com" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Envía una solicitud de logout autenticada.
    /// </summary>
    /// <param name="client">Cliente HTTP que representa un equipo.</param>
    /// <param name="token">JWT Bearer actual.</param>
    /// <returns>Respuesta HTTP del logout.</returns>
    private static async Task<HttpResponseMessage> SendLogoutAsync(HttpClient client, string token)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
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
            builder.UseSetting("UserInvitation:TokenLifetimeHours", "24");
            builder.UseSetting("UserInvitation:ActivationBaseUrl", "https://client.test/activate");
            builder.UseSetting("Email:Smtp:Host", "localhost");
            builder.UseSetting("Email:Smtp:Port", "587");
            builder.UseSetting("Email:Smtp:SecurityMode", "StartTls");
            builder.UseSetting("Email:Smtp:Username", "test@example.com");
            builder.UseSetting("Email:Smtp:Password", "test-provider-secret");
            builder.UseSetting("Email:Smtp:FromAddress", "test@example.com");
            builder.UseSetting("Email:Smtp:TimeoutSeconds", "5");
        }
    }
}
