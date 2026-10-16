using System.Net;
using System.Net.Sockets;
using Fepp.ContaOps.Application.Abstractions;
using Fepp.ContaOps.Application.Results;
using Fepp.ContaOps.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace Fepp.ContaOps.UnitTests;

/// <summary>
/// Verifica el transporte SMTP sin conectarse a Gmail ni a otro proveedor real.
/// </summary>
public sealed class MailKitSmtpEmailSenderTests
{
    /// <summary>
    /// Comprueba que la configuración rechaza SMTP sin cifrado o con puerto incompatible.
    /// </summary>
    [Fact]
    public void SmtpOptions_RejectsUnencryptedOrIncompatibleConnections()
    {
        EmailSmtpOptions options = new()
        {
            Host = "localhost",
            Port = 25,
            SecurityMode = SmtpSecurityMode.StartTls,
            Username = "sender@example.com",
            Password = "provider-secret",
            FromAddress = "sender@example.com",
            TimeoutSeconds = 5
        };

        Assert.False(options.IsValid());

        options.Port = 465;
        Assert.False(options.IsValid());
        options.SecurityMode = SmtpSecurityMode.TlsOnConnect;
        Assert.True(options.IsValid());
    }

    /// <summary>
    /// Comprueba que un fallo de un servidor SMTP en proceso se traduce al error neutral.
    /// </summary>
    [Fact]
    public async Task InProcessSmtpFailure_IsTranslatedToUnavailableError()
    {
        await using InProcessSmtpFailureServer server = new();
        EmailSmtpOptions options = new()
        {
            Host = IPAddress.Loopback.ToString(),
            Port = server.Port,
            SecurityMode = SmtpSecurityMode.StartTls,
            Username = "sender@example.com",
            Password = "provider-secret",
            FromAddress = "sender@example.com",
            TimeoutSeconds = 2
        };
        MailKitSmtpEmailSender sender = new(Options.Create(options));
        EmailMessage message = new(
            "recipient@example.com",
            "Test",
            "<p>HTML</p>",
            "Text",
            "attempt-test");

        Task serverTask = server.AcceptAndCloseAsync();
        await Assert.ThrowsAsync<EmailDeliveryUnavailableException>(
            () => sender.SendAsync(message));
        await serverTask;
    }

    /// <summary>
    /// Servidor SMTP mínimo que acepta una conexión y la cierra para provocar un fallo controlado.
    /// </summary>
    private sealed class InProcessSmtpFailureServer : IAsyncDisposable
    {
        /// <summary>Listener local del servidor de prueba.</summary>
        private readonly TcpListener _listener;

        /// <summary>
        /// Inicializa el servidor en un puerto efímero del loopback.
        /// </summary>
        public InProcessSmtpFailureServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
        }

        /// <summary>Obtiene el puerto local asignado.</summary>
        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        /// <summary>
        /// Acepta una conexión, envía el saludo SMTP y cierra el socket.
        /// </summary>
        /// <returns>Operación de servidor completada.</returns>
        public async Task AcceptAndCloseAsync()
        {
            using TcpClient client = await _listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();
            byte[] greeting = "220 in-process-test\r\n"u8.ToArray();
            await stream.WriteAsync(greeting);
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            _listener.Stop();
            return ValueTask.CompletedTask;
        }
    }
}
