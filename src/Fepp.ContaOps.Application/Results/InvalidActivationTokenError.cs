namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Representa cualquier fallo al validar una invitación de activación.
/// </summary>
public sealed record InvalidActivationTokenError : ApplicationError
{
    /// <summary>
    /// Obtiene la única instancia del error para evitar mensajes variables.
    /// </summary>
    public static InvalidActivationTokenError Instance { get; } = new();

    /// <inheritdoc />
    public override string Code => "invalid_activation_token";

    /// <inheritdoc />
    public override string Message => "The activation token is invalid or expired.";

    /// <summary>
    /// Inicializa el error de token inválido.
    /// </summary>
    private InvalidActivationTokenError()
    {
    }
}
