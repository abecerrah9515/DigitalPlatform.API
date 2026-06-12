using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CargaArchivoController : ControllerBase
{
    private readonly ICargaArchivoService _service;

    public CargaArchivoController(ICargaArchivoService service)
    {
        _service = service;
    }

    [HttpGet("historial")]
    public async Task<IActionResult> ObtenerHistorial()
    {
        var result = await _service.ObtenerHistorialAsync();
        return Ok(result);
    }

    [HttpPost("upload/{tipo}")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> SubirArchivo(string tipo, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Debe seleccionar un archivo.");

        using var stream = file.OpenReadStream();
        var result = await _service.SubirArchivoAsync(tipo, stream, file.FileName);
        return Ok(result);
    }
}
