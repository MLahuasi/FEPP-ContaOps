namespace Fepp.ContaOps.Domain.Users;

/// <summary>
/// Identificador estable de un usuario.
/// </summary>
/// <param name="Value">Valor del identificador.</param>
public readonly record struct UserId(string Value)
{
    /// <summary>
    /// Indica si el identificador contiene un valor válido.
    /// </summary>
    /// <returns><see langword="true"/> cuando el valor no está vacío.</returns>
    public bool IsValid() => !string.IsNullOrWhiteSpace(Value);
}
