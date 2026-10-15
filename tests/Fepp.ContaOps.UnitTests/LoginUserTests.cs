using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Users;
using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica el caso de uso de inicio de sesión sin depender de Identity ni de PostgreSQL.
/// </summary>
public sealed class LoginUserTests
{
    /// <summary>
    /// Comprueba que unas credenciales válidas producen un token.
    /// </summary>
    [Fact]
    public async Task ValidCredentials_ReturnsToken()
    {
        FakeUserRepository repository = new(new User(new UserId("user-1"), "user@example.com", ["User"]));
        FakeCredentialService credentials = new(true);
        FakeTokenIssuer issuer = new();
        LoginUser login = new(repository, credentials, issuer);

        var result = await login.ExecuteAsync("user@example.com", "Password123!");

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value!.AccessToken);
        Assert.Equal(900, result.Value.ExpiresIn);
    }

    /// <summary>
    /// Comprueba que un usuario inexistente no permite emitir un token.
    /// </summary>
    [Fact]
    public async Task UnknownUser_ReturnsInvalidCredentials()
    {
        LoginUser login = new(new FakeUserRepository(null), new FakeCredentialService(true), new FakeTokenIssuer());

        var result = await login.ExecuteAsync("unknown@example.com", "Password123!");

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_credentials", result.Error!.Code);
    }

    /// <summary>
    /// Comprueba que una contraseña incorrecta no permite emitir un token.
    /// </summary>
    [Fact]
    public async Task InvalidPassword_ReturnsInvalidCredentials()
    {
        User user = new(new UserId("user-1"), "user@example.com", ["User"]);
        LoginUser login = new(new FakeUserRepository(user), new FakeCredentialService(false), new FakeTokenIssuer());

        var result = await login.ExecuteAsync(user.Email, "wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_credentials", result.Error!.Code);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user)
        {
            _user = user;
        }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user);
        }
    }

    private sealed class FakeCredentialService : IUserCredentialService
    {
        private readonly bool _isValid;

        public FakeCredentialService(bool isValid)
        {
            _isValid = isValid;
        }

        public Task<bool> VerifyPasswordAsync(
            User user,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_isValid);
        }
    }

    private sealed class FakeTokenIssuer : ITokenIssuer
    {
        public Task<TokenResult> IssueAsync(User user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TokenResult("token", 900));
        }
    }
}
