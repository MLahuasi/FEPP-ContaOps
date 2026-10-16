using System.Text.Json;
using Fepp.ContaOps.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace Fepp.ContaOps.Infrastructure.Email.Templates;

/// <summary>
/// Catálogo validado de plantillas de correo distribuidas con la aplicación.
/// </summary>
public sealed class InvitationTemplateCatalog
{
    /// <summary>
    /// Nombre del manifest del catálogo.
    /// </summary>
    private const string ManifestFileName = "templates.json";

    /// <summary>
    /// Definiciones cargadas desde el manifest.
    /// </summary>
    private readonly IReadOnlyDictionary<string, EmailTemplateDefinition> _definitions;

    /// <summary>
    /// Directorio raíz que contiene el catálogo validado.
    /// </summary>
    private readonly string _rootPath;

    /// <summary>
    /// Carga y valida el manifest y todos sus archivos.
    /// </summary>
    /// <param name="options">Configuración del directorio de plantillas.</param>
    public InvitationTemplateCatalog(IOptions<EmailTemplatesOptions> options)
    {
        _rootPath = ResolveRootPath(options.Value.RootPath);
        _definitions = LoadDefinitions(_rootPath);
    }

    /// <summary>
    /// Obtiene la definición de una plantilla por su clave.
    /// </summary>
    /// <param name="templateKey">Clave declarada en el manifest.</param>
    /// <returns>Definición validada de la plantilla.</returns>
    public EmailTemplateDefinition Get(string templateKey)
    {
        return _definitions.TryGetValue(templateKey, out EmailTemplateDefinition? definition)
            ? definition
            : throw new InvalidOperationException(
                $"Email template '{templateKey}' is not configured.");
    }

    /// <summary>
    /// Lee el contenido de una variante de plantilla.
    /// </summary>
    /// <param name="templateKey">Clave declarada en el manifest.</param>
    /// <param name="html">Indica si se debe leer la variante HTML.</param>
    /// <returns>Contenido de la plantilla.</returns>
    public string Read(string templateKey, bool html)
    {
        EmailTemplateDefinition definition = Get(templateKey);
        string fileName = html ? definition.HtmlFile : definition.TextFile;
        return File.ReadAllText(Path.Combine(_rootPath, fileName));
    }

    /// <summary>
    /// Resuelve el directorio configurado o el catálogo distribuido por defecto.
    /// </summary>
    /// <param name="configuredRootPath">Ruta opcional de configuración.</param>
    /// <returns>Ruta absoluta del catálogo.</returns>
    private static string ResolveRootPath(string configuredRootPath)
    {
        string rootPath = string.IsNullOrWhiteSpace(configuredRootPath)
            ? Path.Combine(
                Path.GetDirectoryName(typeof(InvitationTemplateCatalog).Assembly.Location)
                    ?? AppContext.BaseDirectory,
                "Email",
                "Templates")
            : configuredRootPath;

        return Path.GetFullPath(rootPath);
    }

    /// <summary>
    /// Carga y valida todas las definiciones del manifest.
    /// </summary>
    /// <param name="rootPath">Directorio raíz del catálogo.</param>
    /// <returns>Definiciones indexadas por clave.</returns>
    private static IReadOnlyDictionary<string, EmailTemplateDefinition> LoadDefinitions(
        string rootPath)
    {
        string manifestPath = Path.Combine(rootPath, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            throw new InvalidOperationException($"Email template manifest is missing: {manifestPath}.");
        }

        Dictionary<string, EmailTemplateDefinition>? definitions =
            JsonSerializer.Deserialize<Dictionary<string, EmailTemplateDefinition>>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (definitions is null || definitions.Count == 0)
        {
            throw new InvalidOperationException("Email template manifest must contain at least one template.");
        }

        foreach ((string key, EmailTemplateDefinition definition) in definitions)
        {
            ValidateDefinition(rootPath, key, definition);
        }

        return definitions;
    }

    /// <summary>
    /// Valida una definición, sus archivos y sus parámetros declarados.
    /// </summary>
    /// <param name="rootPath">Directorio raíz del catálogo.</param>
    /// <param name="key">Clave de la plantilla.</param>
    /// <param name="definition">Definición a validar.</param>
    private static void ValidateDefinition(
        string rootPath,
        string key,
        EmailTemplateDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.HtmlFile)
            || string.IsNullOrWhiteSpace(definition.TextFile)
            || string.IsNullOrWhiteSpace(definition.Subject)
            || definition.Parameters is null)
        {
            throw new InvalidOperationException(
                $"Email template '{key}' must define HTML, text, subject, and parameters.");
        }

        string htmlPath = GetSafePath(rootPath, definition.HtmlFile, key);
        string textPath = GetSafePath(rootPath, definition.TextFile, key);
        if (!File.Exists(htmlPath) || !File.Exists(textPath))
        {
            throw new InvalidOperationException(
                $"Email template '{key}' references a missing HTML or text file.");
        }

        string html = File.ReadAllText(htmlPath);
        string text = File.ReadAllText(textPath);
        foreach (string parameter in definition.Parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter)
                || !html.Contains($"{{{{{parameter}}}}}", StringComparison.Ordinal)
                || !text.Contains($"{{{{{parameter}}}}}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Email template '{key}' is missing declared parameter '{parameter}'.");
            }
        }
    }

    /// <summary>
    /// Obtiene una ruta de archivo contenida dentro del catálogo configurado.
    /// </summary>
    /// <param name="rootPath">Directorio raíz permitido.</param>
    /// <param name="fileName">Nombre relativo declarado en el manifest.</param>
    /// <param name="templateKey">Clave de la plantilla.</param>
    /// <returns>Ruta absoluta segura.</returns>
    private static string GetSafePath(string rootPath, string fileName, string templateKey)
    {
        string fullPath = Path.GetFullPath(Path.Combine(rootPath, fileName));
        string normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Email template '{templateKey}' references a file outside its root path.");
        }

        return fullPath;
    }
}

/// <summary>
/// Definición de una plantilla multipart en el manifest.
/// </summary>
public sealed class EmailTemplateDefinition
{
    /// <summary>Archivo de la variante HTML.</summary>
    public string HtmlFile { get; set; } = string.Empty;

    /// <summary>Archivo de la variante de texto plano.</summary>
    public string TextFile { get; set; } = string.Empty;

    /// <summary>Asunto del mensaje.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Parámetros que deben aparecer en ambas variantes.</summary>
    public string[] Parameters { get; set; } = [];
}
