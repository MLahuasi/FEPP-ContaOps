namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Representa un conflicto entre la operación solicitada y el estado actual.
/// </summary>
public sealed record ConflictError : ApplicationError
{
    /// <summary>
    /// Inicializa un error de conflicto.
    /// </summary>
    /// <param name="message">Mensaje seguro que describe el conflicto.</param>
    public ConflictError(string message)
    {
        Message = message;
    }

    /// <inheritdoc />
    public override string Code => "resource_conflict";

    /// <inheritdoc />
    public override string Message { get; }
}
