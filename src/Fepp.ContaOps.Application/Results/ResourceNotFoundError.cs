namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Representa un recurso que no existe para la operación solicitada.
/// </summary>
/// <param name="ResourceName">Nombre seguro del tipo de recurso.</param>
/// <param name="Identifier">Identificador seguro utilizado en la búsqueda.</param>
public sealed record ResourceNotFoundError(
    string ResourceName,
    string Identifier) : ApplicationError
{
    /// <inheritdoc />
    public override string Code => "resource_not_found";

    /// <inheritdoc />
    public override string Message => $"The requested {ResourceName} was not found.";
}
