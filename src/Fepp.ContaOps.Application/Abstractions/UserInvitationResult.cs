using Fepp.ContaOps.Domain.Users;

namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Resultado interno de una invitación lista para ser notificada.
/// </summary>
/// <param name="User">Usuario al que pertenece la invitación.</param>
/// <param name="ActivationToken">Token de activación que solo circula internamente.</param>
public sealed record UserInvitationResult(
    User User,
    string ActivationToken);
