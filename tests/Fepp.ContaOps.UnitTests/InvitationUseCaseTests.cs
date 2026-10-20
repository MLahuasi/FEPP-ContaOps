using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Application.Users.ActivateUser;
using Fepp.ContaOps.Application.Users.CreateUserInvitation;
using Fepp.ContaOps.Application.Users.ResendUserInvitation;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica la orquestación de los casos de uso de invitaciones.
/// </summary>
public sealed class InvitationUseCaseTests
{
    /// <summary>
    /// Comprueba que la creación notifica únicamente después de obtener la invitación.
    /// </summary>
    [Fact]
    public async Task CreateInvitation_NotifiesAfterRegistration()
    {
        UserInvitationResult invitation = CreateInvitation();
        FakeInvitationService service = new(Result<UserInvitationResult>.Success(invitation));
        FakeInvitationNotifier notifier = new();

        CreateUserInvitation useCase = new(service, notifier);
        Result<UserInvitationResult> result = await useCase.ExecuteAsync(
            new CreateUserInvitationInput(invitation.User.Email));

        Assert.True(result.IsSuccess);
        Assert.Same(invitation, notifier.LastInvitation);
        Assert.Equal(invitation.User.Email, service.LastEmail);
    }

    /// <summary>
    /// Comprueba que un fallo de correo se propaga sin ocultar el error operativo.
    /// </summary>
    [Fact]
    public async Task CreateInvitation_WhenNotifierFails_PropagatesDeliveryError()
    {
        FakeInvitationService service = new(Result<UserInvitationResult>.Success(CreateInvitation()));
        FakeInvitationNotifier notifier = new(new EmailDeliveryUnavailableException());
        CreateUserInvitation useCase = new(service, notifier);

        await Assert.ThrowsAsync<EmailDeliveryUnavailableException>(() => useCase.ExecuteAsync(
            new CreateUserInvitationInput("user@example.com")));
    }

    /// <summary>
    /// Comprueba que la activación delega el token y la contraseña al puerto.
    /// </summary>
    [Fact]
    public async Task ActivateUser_DelegatesToInvitationService()
    {
        User user = new(new UserId("user-1"), "user@example.com", ["User"]);
        FakeInvitationService service = new(Result<User>.Success(user));
        ActivateUser useCase = new(service);

        Result<User> result = await useCase.ExecuteAsync(
            new ActivateUserInput(new UserId("user-1"), "token", "Password123!"));

        Assert.True(result.IsSuccess);
        Assert.Equal("token", service.LastToken);
        Assert.Equal("Password123!", service.LastPassword);
    }

    /// <summary>
    /// Comprueba que el reenvío notifica la nueva invitación.
    /// </summary>
    [Fact]
    public async Task ResendInvitation_NotifiesNewInvitation()
    {
        UserInvitationResult invitation = CreateInvitation();
        FakeInvitationService service = new(Result<UserInvitationResult>.Success(invitation));
        FakeInvitationNotifier notifier = new();
        ResendUserInvitation useCase = new(service, notifier);

        Result<UserInvitationResult> result = await useCase.ExecuteAsync(
            new ResendUserInvitationInput(invitation.User.Id));

        Assert.True(result.IsSuccess);
        Assert.Same(invitation, notifier.LastInvitation);
    }

    private static UserInvitationResult CreateInvitation()
    {
        return new UserInvitationResult(
            new User(new UserId("user-1"), "user@example.com", ["User"], UserStatus.PendingActivation),
            "opaque-token");
    }

    private sealed class FakeInvitationService : IUserInvitationService
    {
        private readonly object _result;

        public FakeInvitationService(object result)
        {
            _result = result;
        }

        public string? LastEmail { get; private set; }

        public string? LastToken { get; private set; }

        public Task<Result<UserInvitationResult>> RegisterPendingAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            LastEmail = email;
            return Task.FromResult((Result<UserInvitationResult>)_result);
        }

        public Task<Result<User>> ActivateAsync(
            UserId userId,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            LastToken = token;
            LastPassword = newPassword;
            return Task.FromResult((Result<User>)_result);
        }

        public string? LastPassword { get; private set; }

        public Task<Result<UserInvitationResult>> ResendAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((Result<UserInvitationResult>)_result);
        }
    }

    private sealed class FakeInvitationNotifier : IInvitationNotifier
    {
        private readonly Exception? _exception;

        public FakeInvitationNotifier(Exception? exception = null)
        {
            _exception = exception;
        }

        public UserInvitationResult? LastInvitation { get; private set; }

        public Task NotifyAsync(
            UserInvitationResult invitation,
            CancellationToken cancellationToken = default)
        {
            LastInvitation = invitation;
            return _exception is null
                ? Task.CompletedTask
                : Task.FromException(_exception);
        }
    }
}
