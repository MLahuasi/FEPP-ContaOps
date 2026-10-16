using System.Net;

namespace Fepp.ContaOps.Infrastructure.Email.Templates;

/// <summary>
/// Renderiza las variantes HTML y texto de una plantilla con parámetros explícitos.
/// </summary>
public sealed class InvitationTemplateRenderer
{
    /// <summary>Catálogo validado utilizado para cargar las plantillas.</summary>
    private readonly InvitationTemplateCatalog _catalog;

    /// <summary>
    /// Inicializa el renderer.
    /// </summary>
    /// <param name="catalog">Catálogo validado de plantillas.</param>
    public InvitationTemplateRenderer(InvitationTemplateCatalog catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// Renderiza el asunto, HTML y texto plano de una plantilla.
    /// </summary>
    /// <param name="templateKey">Clave de la plantilla.</param>
    /// <param name="parameters">Valores de los parámetros declarados.</param>
    /// <returns>Mensaje renderizado sin dependencia del transporte.</returns>
    public RenderedEmailTemplate Render(
        string templateKey,
        IReadOnlyDictionary<string, string> parameters)
    {
        EmailTemplateDefinition definition = _catalog.Get(templateKey);
        foreach (string parameter in definition.Parameters)
        {
            if (!parameters.ContainsKey(parameter))
            {
                throw new InvalidOperationException(
                    $"Email template parameter '{parameter}' was not provided.");
            }
        }

        string html = ReplaceParameters(_catalog.Read(templateKey, html: true), parameters, html: true);
        string text = ReplaceParameters(_catalog.Read(templateKey, html: false), parameters, html: false);
        return new RenderedEmailTemplate(definition.Subject, html, text);
    }

    /// <summary>
    /// Sustituye parámetros aplicando codificación según el contexto.
    /// </summary>
    /// <param name="template">Contenido de la plantilla.</param>
    /// <param name="parameters">Valores a sustituir.</param>
    /// <param name="html">Indica si se aplica codificación HTML.</param>
    /// <returns>Contenido renderizado.</returns>
    private static string ReplaceParameters(
        string template,
        IReadOnlyDictionary<string, string> parameters,
        bool html)
    {
        string rendered = template;
        foreach ((string name, string value) in parameters)
        {
            string encodedValue = html ? WebUtility.HtmlEncode(value) : value;
            rendered = rendered.Replace(
                $"{{{{{name}}}}}",
                encodedValue,
                StringComparison.Ordinal);
        }

        return rendered;
    }
}

/// <summary>
/// Resultado de renderizar una plantilla de correo.
/// </summary>
/// <param name="Subject">Asunto del correo.</param>
/// <param name="HtmlBody">Cuerpo HTML.</param>
/// <param name="TextBody">Cuerpo de texto plano.</param>
public sealed record RenderedEmailTemplate(
    string Subject,
    string HtmlBody,
    string TextBody);
