namespace Fepp.ContaOps.Infrastructure.Identity;

/// <summary>
/// Configuración de la política de contraseñas de ASP.NET Core Identity.
/// </summary>
public sealed class IdentityPasswordOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "Identity:Password";

    /// <summary>
    /// Longitud mínima de la contraseña.
    /// </summary>
    public int RequiredLength { get; set; } = 12;

    /// <summary>
    /// Indica si se requiere una letra mayúscula.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Indica si se requiere una letra minúscula.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Indica si se requiere un dígito.
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Indica si se requiere un carácter no alfanumérico.
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; } = true;

    /// <summary>
    /// Número mínimo de caracteres únicos.
    /// </summary>
    public int RequiredUniqueChars { get; set; } = 6;

    /// <summary>
    /// Indica si la configuración tiene valores compatibles con Identity.
    /// </summary>
    /// <returns><see langword="true"/> cuando los límites son válidos.</returns>
    public bool IsValid()
    {
        return RequiredLength > 0
            && RequiredUniqueChars >= 0
            && RequiredUniqueChars <= RequiredLength;
    }
}
