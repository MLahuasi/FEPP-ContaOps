namespace Fepp.ContaOps.Api.Contracts.Requests;

/// <summary>
/// Datos enviados para renovar una sesión.
/// </summary>
public sealed record RefreshRequest
{
    /// <summary>
    /// Refresh token opaco presentado por el cliente.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
