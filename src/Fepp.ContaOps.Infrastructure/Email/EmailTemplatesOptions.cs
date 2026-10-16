namespace Fepp.ContaOps.Infrastructure.Email;

/// <summary>
/// Configuración del catálogo de plantillas de correo.
/// </summary>
public sealed class EmailTemplatesOptions
{
    /// <summary>
    /// Nombre de la sección de configuración.
    /// </summary>
    public const string SectionName = "Email:Templates";

    /// <summary>
    /// Directorio alternativo del catálogo de plantillas.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;
}
