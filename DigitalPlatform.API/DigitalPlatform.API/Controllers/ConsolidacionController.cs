using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Consolidacion;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsolidacionController : ControllerBase
{
    private readonly IConsolidacionService _consolidacionService;

    public ConsolidacionController(IConsolidacionService consolidacionService)
    {
        _consolidacionService = consolidacionService;
    }

    // POST api/consolidacion/iniciar
    [HttpPost("iniciar")]
    public async Task<ActionResult<ApiResponse<ConsolidacionIniciadaDto>>> Iniciar()
    {
        var resultado = await _consolidacionService.IniciarConsolidacionAsync("sistema");
        return Ok(resultado);
    }

    // GET api/consolidacion/{id}/estado
    [HttpGet("{id}/estado")]
    public async Task<ActionResult<ApiResponse<ConsolidacionEstadoDto>>> ObtenerEstado(int id)
    {
        var resultado = await _consolidacionService.ObtenerEstadoAsync(id);
        return Ok(resultado);
    }

    // GET api/consolidacion/historial
    [HttpGet("historial")]
    public async Task<ActionResult<ApiResponse<PagedResult<ConsolidacionHistorialDto>>>> ObtenerHistorial(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamañoPagina = 10)
    {
        var resultado = await _consolidacionService.ObtenerHistorialAsync(pagina, tamañoPagina);
        return Ok(resultado);
    }
}
