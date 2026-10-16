using System.Net;
using System.Net.Sockets;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Fepp.ContaOps.Infrastructure.Email;

/// <summary>
/// Entrega mensajes multipart mediante SMTP usando MailKit.
/// </summary>
public sealed class MailKitSmtpEmailSender : IEmailSender
{
    /// <summary>Configuración neutral del transporte SMTP.</summary>
    private readonly EmailSmtpOptions _options;

    /// <summary>
    /// Inicializa el adaptador SMTP.
    /// </summary>
    /// <param name="options">Configuración validada del transporte.</param>
    public MailKitSmtpEmailSender(IOptions<EmailSmtpOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeoutSource = new(
            TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);

        using SmtpClient client = new();
        client.Timeout = checked(_options.TimeoutSeconds * 1000);
        MimeMessage mimeMessage = BuildMimeMessage(message);

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                ToSecureSocketOptions(),
                linkedCancellation.Token);
            await client.AuthenticateAsync(
                _options.Username,
                _options.Password,
                linkedCancellation.Token);
            await client.SendAsync(mimeMessage, linkedCancellation.Token);
            await client.DisconnectAsync(quit: true, linkedCancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmailDeliveryUnavailableException();
        }
        catch (Exception exception) when (IsDeliveryFailure(exception))
        {
            throw new EmailDeliveryUnavailableException();
        }
    }

    /// <summary>
    /// Construye el mensaje multipart con las variantes HTML y texto plano.
    /// </summary>
    /// <param name="message">Mensaje neutral de Application.</param>
    /// <returns>Mensaje MIME listo para SMTP.</returns>
    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        MimeMessage mimeMessage = new();
        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        BodyBuilder bodyBuilder = new()
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody
        };
        mimeMessage.Body = bodyBuilder.ToMessageBody();
        return mimeMessage;
    }

    /// <summary>
    /// Traduce el modo neutral de seguridad al enum de MailKit.
    /// </summary>
    /// <returns>Modo TLS de MailKit.</returns>
    private SecureSocketOptions ToSecureSocketOptions()
    {
        return _options.SecurityMode switch
        {
            SmtpSecurityMode.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurityMode.TlsOnConnect => SecureSocketOptions.SslOnConnect,
            _ => throw new EmailDeliveryUnavailableException()
        };
    }

    /// <summary>
    /// Identifica fallos de transporte sin registrar sus detalles.
    /// </summary>
    /// <param name="exception">Excepción producida por MailKit o la red.</param>
    /// <returns><see langword="true"/> cuando el fallo debe ser neutralizado.</returns>
    private static bool IsDeliveryFailure(Exception exception)
    {
        return exception is AuthenticationException
            or IOException
            or SocketException
            or SmtpCommandException
            or SmtpProtocolException
            or MailKit.Security.SslHandshakeException
            or TimeoutException
            or FormatException
            or ParseException;
    }
}
