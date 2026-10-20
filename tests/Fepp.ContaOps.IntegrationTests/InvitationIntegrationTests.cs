using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Fepp.ContaOps.IntegrationTests;

/// <summary>
/// Verifica el flujo HTTP de invitaciones con PostgreSQL efímero y correo falso.
/// </summary>
public sealed class InvitationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16-alpine").Build();

    /// <summary>
    /// Inicia PostgreSQL antes de ejecutar las pruebas.
    /// </summary>
    public Task InitializeAsync()
    {
        return _database.StartAsync();
    }

    /// <summary>
    /// Libera PostgreSQL después de ejecutar las pruebas.
    /// </summary>
    public Task DisposeAsync()
    {
        return _database.DisposeAsync().AsTask();
    }

    /// <summary>
    /// Comprueba la creación, activación y login posterior de una cuenta invitada.
    /// </summary>
    [Fact]
    public async Task AdminCreatesInvitation_ActivationAllowsLogin()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        HttpResponseMessage createResponse = await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        string createBody = await createResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        FakeInvitationNotifier notifier = factory.Services.GetRequiredService<FakeInvitationNotifier>();
        Assert.NotNull(notifier.LastInvitation);
        UserInvitationResult invitation = notifier.LastInvitation!;
        Assert.Contains("\"role\":\"User\"", createBody, StringComparison.Ordinal);
        Assert.DoesNotContain(invitation.ActivationToken, createBody, StringComparison.Ordinal);
        Assert.DoesNotContain("activationLink", createBody, StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage pendingLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, pendingLogin.StatusCode);

        HttpResponseMessage activationResponse = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.NoContent, activationResponse.StatusCode);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    /// <summary>
    /// Comprueba que los endpoints administrativos exigen autenticación y rol Admin.
    /// </summary>
    [Fact]
    public async Task InvitationEndpoints_RequireAdminRole()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);

        HttpResponseMessage withoutToken = await client.PostAsJsonAsync(
            "/api/users/invitations",
            new { email = UniqueEmail() });
        Assert.Equal(HttpStatusCode.Unauthorized, withoutToken.StatusCode);
        Assert.Equal("authentication_required", await ReadCodeAsync(withoutToken));

        string email = UniqueEmail();
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        FakeInvitationNotifier notifier = factory.Services.GetRequiredService<FakeInvitationNotifier>();
        Assert.NotNull(notifier.LastInvitation);
        UserInvitationResult invitation = notifier.LastInvitation!;

        HttpResponseMessage activation = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);
        string userToken = await LoginAsync(client, email, "Password123!");

        HttpResponseMessage asUser = await SendAsAdminAsync(
            client,
            userToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email = UniqueEmail() }));
        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);
        Assert.Equal("forbidden", await ReadCodeAsync(asUser));
    }

    /// <summary>
    /// Comprueba que un email duplicado produce conflicto.
    /// </summary>
    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string token = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        HttpResponseMessage first = await SendAsAdminAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        HttpResponseMessage second = await SendAsAdminAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("resource_conflict", await ReadCodeAsync(second));
    }

    /// <summary>
    /// Comprueba que el reenvío invalida el token anterior.
    /// </summary>
    [Fact]
    public async Task ResendInvitation_InvalidatesPreviousToken()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        FakeInvitationNotifier notifier = factory.Services.GetRequiredService<FakeInvitationNotifier>();
        Assert.NotNull(notifier.LastInvitation);
        UserInvitationResult firstInvitation = notifier.LastInvitation!;

        HttpResponseMessage resend = await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            $"/api/users/{firstInvitation.User.Id.Value}/invitations",
            content: null);
        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);
        Assert.NotNull(notifier.LastInvitation);
        UserInvitationResult secondInvitation = notifier.LastInvitation!;

        HttpResponseMessage oldToken = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = firstInvitation.User.Id.Value,
                token = firstInvitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.BadRequest, oldToken.StatusCode);
        Assert.Equal("invalid_activation_token", await ReadCodeAsync(oldToken));

        HttpResponseMessage newToken = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = secondInvitation.User.Id.Value,
                token = secondInvitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.NoContent, newToken.StatusCode);
    }

    /// <summary>
    /// Comprueba que un token consumido no puede activar nuevamente la cuenta.
    /// </summary>
    [Fact]
    public async Task UsedInvitationToken_CannotBeConsumedAgain()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));

        UserInvitationResult invitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        HttpResponseMessage firstActivation = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.NoContent, firstActivation.StatusCode);

        HttpResponseMessage secondActivation = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "AnotherPassword123!"
            });
        Assert.Equal(HttpStatusCode.BadRequest, secondActivation.StatusCode);
        Assert.Equal("invalid_activation_token", await ReadCodeAsync(secondActivation));
    }

    /// <summary>
    /// Comprueba que dos activaciones concurrentes no pueden consumir la misma invitación.
    /// </summary>
    [Fact]
    public async Task ConcurrentActivations_OnlyOneSucceeds()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        UserInvitationResult invitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        Task<HttpResponseMessage> firstActivation = client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });
        Task<HttpResponseMessage> secondActivation = client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });

        HttpResponseMessage[] responses = await Task.WhenAll(firstActivation, secondActivation);
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));

        HttpResponseMessage failedResponse = responses.Single(
            response => response.StatusCode == HttpStatusCode.BadRequest);
        Assert.Equal("invalid_activation_token", await ReadCodeAsync(failedResponse));
        foreach (HttpResponseMessage response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Comprueba que una cuenta activa no puede recibir un reenvío.
    /// </summary>
    [Fact]
    public async Task ResendActiveInvitation_ReturnsConflict()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        UserInvitationResult invitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        HttpResponseMessage activation = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);

        HttpResponseMessage resend = await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            $"/api/users/{invitation.User.Id.Value}/invitations",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, resend.StatusCode);
        Assert.Equal("resource_conflict", await ReadCodeAsync(resend));
    }

    /// <summary>
    /// Comprueba que un identificador inexistente produce un recurso no encontrado.
    /// </summary>
    [Fact]
    public async Task ResendUnknownInvitation_ReturnsNotFound()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");

        HttpResponseMessage resend = await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            $"/api/users/{Guid.NewGuid()}/invitations",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, resend.StatusCode);
        Assert.Equal("resource_not_found", await ReadCodeAsync(resend));
    }

    /// <summary>
    /// Comprueba que una contraseña rechazada por Identity no consume la invitación.
    /// </summary>
    [Fact]
    public async Task InvalidActivationPassword_ReturnsValidationAndPreservesToken()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        UserInvitationResult invitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        HttpResponseMessage invalidActivation = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "simple"
            });
        Assert.Equal(HttpStatusCode.BadRequest, invalidActivation.StatusCode);
        Assert.Equal("validation_error", await ReadCodeAsync(invalidActivation));

        HttpResponseMessage validActivation = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });
        Assert.Equal(HttpStatusCode.NoContent, validActivation.StatusCode);
    }

    /// <summary>
    /// Comprueba que JSON inválido conserva el contrato uniforme de errores.
    /// </summary>
    [Fact]
    public async Task InvalidJson_ReturnsValidationProblemDetails()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        using StringContent content = new("{", System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync("/api/auth/activate", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"code\"", body, StringComparison.Ordinal);
        Assert.Contains("validation_error", body, StringComparison.Ordinal);
        Assert.StartsWith("application/problem+json", response.Content.Headers.ContentType?.ToString());
    }

    /// <summary>
    /// Comprueba que un identificador inválido produce validación uniforme.
    /// </summary>
    [Fact]
    public async Task InvalidActivationUserId_ReturnsValidationProblemDetails()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = "not-a-guid",
                token = "opaque-token",
                newPassword = "Password123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_error", await ReadCodeAsync(response));
    }

    /// <summary>
    /// Comprueba que un token arbitrario usa el mismo error público de activación.
    /// </summary>
    [Fact]
    public async Task InvalidActivationToken_ReturnsUniformError()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        UserInvitationResult invitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = "invalid-token",
                newPassword = "Password123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_activation_token", await ReadCodeAsync(response));
    }

    /// <summary>
    /// Comprueba que un token vencido no puede activar una cuenta pendiente.
    /// </summary>
    [Fact]
    public async Task ExpiredInvitationToken_ReturnsUniformError()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));
        UserInvitationResult invitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        HashedUserToken persistedToken = await dbContext.UserSecurityTokens
            .SingleAsync(token => token.UserId == invitation.User.Id.Value);
        persistedToken.ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await dbContext.SaveChangesAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = invitation.User.Id.Value,
                token = invitation.ActivationToken,
                newPassword = "Password123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_activation_token", await ReadCodeAsync(response));
    }

    /// <summary>
    /// Comprueba que un token no puede utilizarse con otra cuenta pendiente.
    /// </summary>
    [Fact]
    public async Task InvitationTokenFromAnotherUser_ReturnsUniformError()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email = UniqueEmail() }));
        UserInvitationResult firstInvitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email = UniqueEmail() }));
        UserInvitationResult secondInvitation = factory.Services
            .GetRequiredService<FakeInvitationNotifier>()
            .LastInvitation!;

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/activate",
            new
            {
                userId = secondInvitation.User.Id.Value,
                token = firstInvitation.ActivationToken,
                newPassword = "Password123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_activation_token", await ReadCodeAsync(response));
    }

    /// <summary>
    /// Comprueba que un fallo de correo devuelve 503 y conserva la cuenta pendiente.
    /// </summary>
    [Fact]
    public async Task NotificationFailure_Returns503AndKeepsPendingAccount()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = CreateHttpsClient(factory);
        string adminToken = await LoginAsync(client, "admin@example.com", "AdminPassword123!");
        string email = UniqueEmail();
        factory.Services.GetRequiredService<FakeInvitationNotifier>().ThrowOnNotify = true;

        HttpResponseMessage create = await SendAsAdminAsync(
            client,
            adminToken,
            HttpMethod.Post,
            "/api/users/invitations",
            JsonContent.Create(new { email }));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, create.StatusCode);
        Assert.Equal("email_delivery_unavailable", await ReadCodeAsync(create));

        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = "Password123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    private TestApiFactory CreateFactory()
    {
        return new TestApiFactory(_database.GetConnectionString());
    }

    private static HttpClient CreateHttpsClient(TestApiFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> SendAsAdminAsync(
        HttpClient client,
        string token,
        HttpMethod method,
        string uri,
        HttpContent? content)
    {
        using HttpRequestMessage request = new(method, uri)
        {
            Content = content
        };
        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        return await client.SendAsync(request);
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString()!;
    }

    private static string UniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@example.com";
    }

    /// <summary>
    /// Fábrica de pruebas con PostgreSQL y notificador en memoria.
    /// </summary>
    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public TestApiFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = _connectionString,
                    ["Jwt:Issuer"] = "FEPP-ContaOps.Tests",
                    ["Jwt:Audience"] = "FEPP-ContaOps.Tests",
                    ["Jwt:SigningKey"] = "test-signing-key-with-at-least-32-bytes",
                    ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                    ["BootstrapAdmin:Email"] = "admin@example.com",
                    ["BootstrapAdmin:Password"] = "AdminPassword123!",
                    ["UserInvitation:TokenLifetimeHours"] = "24",
                    ["UserInvitation:ActivationBaseUrl"] = "https://client.test/activate",
                    ["Email:Smtp:Host"] = "localhost",
                    ["Email:Smtp:Port"] = "587",
                    ["Email:Smtp:SecurityMode"] = "StartTls",
                    ["Email:Smtp:Username"] = "test@example.com",
                    ["Email:Smtp:Password"] = "test-provider-secret",
                    ["Email:Smtp:FromAddress"] = "test@example.com",
                    ["Email:Smtp:TimeoutSeconds"] = "5"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(_connectionString));
                services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ => { });
                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                });
                services.RemoveAll<IInvitationNotifier>();
                services.AddSingleton<FakeInvitationNotifier>();
                services.AddSingleton<IInvitationNotifier>(provider =>
                    provider.GetRequiredService<FakeInvitationNotifier>());
            });
        }
    }

    /// <summary>
    /// Autenticación determinista del host de integración basada en los claims del JWT.
    /// </summary>
    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        /// <summary>
        /// Nombre del esquema de autenticación aislado para las pruebas.
        /// </summary>
        public const string SchemeName = "IntegrationTestBearer";

        /// <summary>
        /// Inicializa el handler de autenticación de pruebas.
        /// </summary>
        /// <param name="options">Opciones del esquema.</param>
        /// <param name="logger">Fábrica de logs.</param>
        /// <param name="encoder">Codificador de URL.</param>
        /// <param name="clock">Reloj del sistema.</param>
#pragma warning disable CS0618
        public TestAuthenticationHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }
#pragma warning restore CS0618

        /// <summary>
        /// Lee el Bearer emitido por el caso de uso de login y crea su identidad de prueba.
        /// </summary>
        /// <returns>Resultado de autenticación.</returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? authorization = Request.Headers.Authorization.ToString();
            if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string token = authorization["Bearer ".Length..].Trim();
            JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            ClaimsIdentity identity = new(
                jwt.Claims,
                SchemeName,
                JwtRegisteredClaimNames.Email,
                ClaimTypes.Role);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>
        /// Devuelve el error uniforme esperado para una solicitud sin identidad.
        /// </summary>
        /// <param name="properties">Propiedades del challenge.</param>
        protected override Task HandleChallengeAsync(AuthenticationProperties? properties)
        {
            return WriteProblemAsync(StatusCodes.Status401Unauthorized, "authentication_required");
        }

        /// <summary>
        /// Devuelve el error uniforme esperado para una identidad sin permisos.
        /// </summary>
        /// <param name="properties">Propiedades del forbid.</param>
        protected override Task HandleForbiddenAsync(AuthenticationProperties? properties)
        {
            return WriteProblemAsync(StatusCodes.Status403Forbidden, "forbidden");
        }

        /// <summary>
        /// Escribe un ProblemDetails mínimo para el host de pruebas.
        /// </summary>
        /// <param name="statusCode">Código HTTP de la respuesta.</param>
        /// <param name="code">Código estable del error.</param>
        private Task WriteProblemAsync(int statusCode, string code)
        {
            Response.StatusCode = statusCode;
            Response.ContentType = "application/problem+json";
            return Response.WriteAsync(JsonSerializer.Serialize(new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title = statusCode == StatusCodes.Status401Unauthorized
                    ? "Authentication failed."
                    : "Access forbidden.",
                status = statusCode,
                code,
                traceId = Context.TraceIdentifier
            }));
        }
    }

    /// <summary>
    /// Notificador en memoria que conserva el token solo para preparar la prueba.
    /// </summary>
    public sealed class FakeInvitationNotifier : IInvitationNotifier
    {
        public UserInvitationResult? LastInvitation { get; private set; }

        public bool ThrowOnNotify { get; set; }

        public Task NotifyAsync(
            UserInvitationResult invitation,
            CancellationToken cancellationToken = default)
        {
            LastInvitation = invitation;
            if (ThrowOnNotify)
            {
                throw new EmailDeliveryUnavailableException();
            }

            return Task.CompletedTask;
        }
    }
}
