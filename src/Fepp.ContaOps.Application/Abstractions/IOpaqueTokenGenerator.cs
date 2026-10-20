namespace Fepp.ContaOps.Application.Abstractions;

/// <summary>
/// Puerto para generar tokens opacos de alta entropía.
/// </summary>
public interface IOpaqueTokenGenerator
{
    /// <summary>
    /// Genera un token público codificado en Base64Url.
    /// </summary>
    /// <returns>Token opaco sin padding Base64Url.</returns>
    string Generate();
}
