namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Representa errores de validación de una entrada o precondición de aplicación.
/// </summary>
/// <param name="Errors">Errores agrupados por nombre de campo.</param>
public sealed record ValidationError(
    IReadOnlyDictionary<string, string[]> Errors) : ApplicationError
{
    /// <inheritdoc />
    public override string Code => "validation_error";

    /// <inheritdoc />
    public override string Message => "One or more validation errors occurred.";
}
