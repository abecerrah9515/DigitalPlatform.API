using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Proyectos;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/kpis")]
public class KpisController : ControllerBase
{
    private readonly IProyectoService _proyectoService;

    public KpisController(IProyectoService proyectoService)
        => _proyectoService = proyectoService;

    // GET /api/kpis
    [HttpGet]
    public async Task<ActionResult<ApiResponse<KpisDto>>> ObtenerKpis(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.ObtenerKpisAsync(filtro);
        return Ok(resultado);
    }
}
