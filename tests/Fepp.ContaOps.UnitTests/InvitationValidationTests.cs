using Fepp.ContaOps.Api.Contracts.Requests;
using Fepp.ContaOps.Api.Validation;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica los límites de transporte de los requests de invitación.
/// </summary>
public sealed class InvitationValidationTests
{
    /// <summary>
    /// Comprueba que el email válido cumple el contrato de creación.
    /// </summary>
    [Fact]
    public async Task CreateInvitation_ValidEmail_IsAccepted()
    {
        CreateUserInvitationRequestValidator validator = new();

        var result = await validator.ValidateAsync(new CreateUserInvitationRequest
        {
            Email = "user@example.com"
        });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Comprueba que el email excedido se rechaza.
    /// </summary>
    [Fact]
    public async Task CreateInvitation_EmailOverLimit_IsRejected()
    {
        CreateUserInvitationRequestValidator validator = new();

        var result = await validator.ValidateAsync(new CreateUserInvitationRequest
        {
            Email = new string('a', 245) + "@example.com"
        });

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Comprueba que la activación acepta límites de transporte válidos.
    /// </summary>
    [Fact]
    public async Task ActivateUser_ValidTransport_IsAccepted()
    {
        ActivateUserRequestValidator validator = new();

        var result = await validator.ValidateAsync(new ActivateUserRequest
        {
            UserId = "00000000-0000-0000-0000-000000000001",
            Token = "opaque-token",
            NewPassword = "short-is-allowed-here"
        });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Comprueba que la complejidad de contraseña no se valida en Api.
    /// </summary>
    [Fact]
    public async Task ActivateUser_DoesNotDuplicateIdentityComplexityRules()
    {
        ActivateUserRequestValidator validator = new();

        var result = await validator.ValidateAsync(new ActivateUserRequest
        {
            UserId = "00000000-0000-0000-0000-000000000001",
            Token = "opaque-token",
            NewPassword = "simple"
        });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Comprueba que el identificador y el token inválidos se rechazan.
    /// </summary>
    [Fact]
    public async Task ActivateUser_InvalidIdentifierAndToken_AreRejected()
    {
        ActivateUserRequestValidator validator = new();

        var result = await validator.ValidateAsync(new ActivateUserRequest
        {
            UserId = "not-a-guid",
            Token = string.Empty,
            NewPassword = "Password123!"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ActivateUserRequest.UserId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ActivateUserRequest.Token));
    }
}
