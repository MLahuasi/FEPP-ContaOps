using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Infrastructure.Identity;
using Fepp.ContaOps.Infrastructure.Email.Templates;
using Microsoft.Extensions.Options;

namespace Fepp.ContaOps.Infrastructure.Email;

/// <summary>
/// Construye la invitación y delega su entrega al transporte de correo.
/// </summary>
public sealed class InvitationEmailNotifier : IInvitationNotifier
{
    /// <summary>Transporte neutral de correo.</summary>
    private readonly IEmailSender _emailSender;

    /// <summary>Renderer del catálogo de plantillas.</summary>
    private readonly InvitationTemplateRenderer _renderer;

    /// <summary>Configuración de la URL cliente.</summary>
    private readonly UserInvitationOptions _options;

    /// <summary>
    /// Inicializa el notificador.
    /// </summary>
    /// <param name="emailSender">Transporte neutral de correo.</param>
    /// <param name="renderer">Renderer de plantillas.</param>
    /// <param name="options">Configuración de invitaciones.</param>
    public InvitationEmailNotifier(
        IEmailSender emailSender,
        InvitationTemplateRenderer renderer,
        IOptions<UserInvitationOptions> options)
    {
        _emailSender = emailSender;
        _renderer = renderer;
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task NotifyAsync(
        UserInvitationResult invitation,
        CancellationToken cancellationToken = default)
    {
        string activationLink = BuildActivationLink(
            invitation.User.Id.Value,
            invitation.ActivationToken);
        RenderedEmailTemplate template = _renderer.Render(
            "invitation",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["userId"] = invitation.User.Id.Value,
                ["activationLink"] = activationLink
            });

        EmailMessage message = new(
            invitation.User.Email,
            template.Subject,
            template.HtmlBody,
            template.TextBody,
            Guid.NewGuid().ToString("N"));
        return _emailSender.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Construye el enlace HTTPS con los datos de activación en el fragmento.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="token">Token de activación.</param>
    /// <returns>Enlace de activación URL-safe.</returns>
    private string BuildActivationLink(string userId, string token)
    {
        return $"{_options.ActivationBaseUrl.TrimEnd('/')}#userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}";
    }
}
