using Microsoft.AspNetCore.Mvc;

namespace Fepp.ContaOps.IntegrationTests;

/// <summary>
/// Endpoint exclusivo de las pruebas para provocar una excepción no controlada.
/// </summary>
[ApiController]
[Route("test-pipeline")]
public sealed class PipelineTestController : ControllerBase
{
    /// <summary>
    /// Provoca una excepción con datos internos para verificar su ocultamiento.
    /// </summary>
    /// <returns>No retorna cuando el pipeline funciona correctamente.</returns>
    [HttpGet("unexpected-error")]
    public IActionResult UnexpectedError()
    {
        throw new InvalidOperationException("InternalOperationDetails");
    }
}
