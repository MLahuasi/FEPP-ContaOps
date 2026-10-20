using Fepp.ContaOps.Infrastructure.Identity;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica los valores y límites de configuración de Identity.
/// </summary>
public sealed class IdentityOptionsTests
{
    /// <summary>
    /// Comprueba los defaults de contraseña definidos por la spec.
    /// </summary>
    [Fact]
    public void PasswordOptions_DefaultsAreSecure()
    {
        IdentityPasswordOptions options = new();

        Assert.Equal(12, options.RequiredLength);
        Assert.True(options.RequireUppercase);
        Assert.True(options.RequireLowercase);
        Assert.True(options.RequireDigit);
        Assert.True(options.RequireNonAlphanumeric);
        Assert.Equal(6, options.RequiredUniqueChars);
        Assert.True(options.IsValid());
    }

    /// <summary>
    /// Comprueba que una política de contraseña inválida se rechaza.
    /// </summary>
    [Fact]
    public void PasswordOptions_InvalidUniqueCharacterLimit_IsRejected()
    {
        IdentityPasswordOptions options = new()
        {
            RequiredLength = 5,
            RequiredUniqueChars = 6
        };

        Assert.False(options.IsValid());
    }

    /// <summary>
    /// Comprueba los defaults de bloqueo definidos por la spec.
    /// </summary>
    [Fact]
    public void LockoutOptions_DefaultsAreSecure()
    {
        IdentityLockoutOptions options = new();

        Assert.True(options.AllowedForNewUsers);
        Assert.Equal(5, options.MaxFailedAccessAttempts);
        Assert.Equal(15, options.DefaultLockoutTimeSpanMinutes);
        Assert.True(options.IsValid());
    }

    /// <summary>
    /// Comprueba que los valores no positivos de bloqueo se rechazan.
    /// </summary>
    [Fact]
    public void LockoutOptions_NonPositiveValues_AreRejected()
    {
        IdentityLockoutOptions options = new()
        {
            MaxFailedAccessAttempts = 0,
            DefaultLockoutTimeSpanMinutes = 0
        };

        Assert.False(options.IsValid());
    }
}
