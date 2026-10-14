namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Representa un error esperado producido durante la ejecución de un caso de uso.
/// </summary>
public abstract record ApplicationError
{
    /// <summary>
    /// Obtiene el código estable que identifica el tipo de error.
    /// </summary>
    public abstract string Code { get; }

    /// <summary>
    /// Obtiene un mensaje seguro para el consumidor del caso de uso.
    /// </summary>
    public abstract string Message { get; }
}
