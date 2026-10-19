using Fepp.ContaOps.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica que la política administrativa exige el rol Admin.
/// </summary>
public sealed class AdminOnlyPolicyTests
{
    /// <summary>
    /// Comprueba que AdminOnly contiene el requisito de rol Admin.
    /// </summary>
    [Fact]
    public async Task AdminOnlyPolicy_RequiresAdminRole()
    {
        Dictionary<string, string?> values = new()
        {
            ["Jwt:Issuer"] = "issuer",
            ["Jwt:Audience"] = "audience",
            ["Jwt:SigningKey"] = "signing-key-with-at-least-32-bytes",
            ["Jwt:AccessTokenLifetimeMinutes"] = "15"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new();
        services.AddApi(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        IAuthorizationPolicyProvider policyProvider = provider
            .GetRequiredService<IAuthorizationPolicyProvider>();
        AuthorizationPolicy? policy = await policyProvider.GetPolicyAsync("AdminOnly");

        Assert.NotNull(policy);
        RolesAuthorizationRequirement requirement = Assert.IsType<RolesAuthorizationRequirement>(
            Assert.Single(policy!.Requirements));
        Assert.Contains("Admin", requirement.AllowedRoles);
    }
}
