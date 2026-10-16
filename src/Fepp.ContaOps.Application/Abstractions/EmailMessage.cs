namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Mensaje de correo independiente del transporte utilizado.
/// </summary>
/// <param name="To">Dirección del destinatario.</param>
/// <param name="Subject">Asunto del mensaje.</param>
/// <param name="HtmlBody">Contenido HTML del mensaje.</param>
/// <param name="TextBody">Contenido de texto plano del mensaje.</param>
/// <param name="AttemptId">Identificador interno del intento de entrega.</param>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody,
    string AttemptId);
