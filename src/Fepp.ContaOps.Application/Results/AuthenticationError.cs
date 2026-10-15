namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Representa credenciales inválidas o una cuenta que no puede autenticarse.
/// </summary>
public sealed record AuthenticationError : ApplicationError
{
    /// <summary>
    /// Obtiene la instancia única del error de autenticación.
    /// </summary>
    public static AuthenticationError Instance { get; } = new();

    /// <inheritdoc />
    public override string Code => "invalid_credentials";

    /// <inheritdoc />
    public override string Message => "The supplied credentials are invalid.";
}
