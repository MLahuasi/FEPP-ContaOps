using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        using HttpClient client = factory.CreateClient();
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
        using HttpClient client = factory.CreateClient();

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
        using HttpClient client = factory.CreateClient();
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
        using HttpClient client = factory.CreateClient();
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
    /// Comprueba que un fallo de correo devuelve 503 y conserva la cuenta pendiente.
    /// </summary>
    [Fact]
    public async Task NotificationFailure_Returns503AndKeepsPendingAccount()
    {
        await using TestApiFactory factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
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
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
                services.RemoveAll<IInvitationNotifier>();
                services.AddSingleton<FakeInvitationNotifier>();
                services.AddSingleton<IInvitationNotifier>(provider =>
                    provider.GetRequiredService<FakeInvitationNotifier>());
            });
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
