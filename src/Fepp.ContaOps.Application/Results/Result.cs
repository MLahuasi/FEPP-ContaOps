namespace Fepp.ContaOps.Application.Results;

/// <summary>
/// Contiene el resultado exitoso o el error esperado de una operación de aplicación.
/// </summary>
/// <typeparam name="TValue">Tipo del valor producido cuando la operación es exitosa.</typeparam>
public sealed record Result<TValue>
{
    private Result(TValue? value, ApplicationError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Obtiene un valor que indica si la operación terminó correctamente.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// Obtiene el valor producido por una operación exitosa.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Obtiene el error producido por una operación fallida.
    /// </summary>
    public ApplicationError? Error { get; }

    /// <summary>
    /// Crea un resultado exitoso.
    /// </summary>
    /// <param name="value">Valor producido por la operación.</param>
    /// <returns>Resultado exitoso que contiene <paramref name="value"/>.</returns>
    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(value, null);
    }

    /// <summary>
    /// Crea un resultado fallido.
    /// </summary>
    /// <param name="error">Error esperado producido por la operación.</param>
    /// <returns>Resultado fallido que contiene <paramref name="error"/>.</returns>
    public static Result<TValue> Failure(ApplicationError error)
    {
        return new Result<TValue>(default, error);
    }
}
