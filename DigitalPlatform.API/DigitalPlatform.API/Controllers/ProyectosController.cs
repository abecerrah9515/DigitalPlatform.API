using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Proyectos;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProyectosController : ControllerBase
{
    private readonly IProyectoService _proyectoService;

    public ProyectosController(IProyectoService proyectoService)
    {
        _proyectoService = proyectoService;
    }

    // GET api/proyectos
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProyectoDto>>>> ObtenerProyectos(
        [FromQuery] ProyectoFiltroDto filtro)
    {
        var resultado = await _proyectoService.ObtenerProyectosAsync(filtro);
        return Ok(resultado);
    }

    // GET api/proyectos/kpis
    [HttpGet("kpis")]
    public async Task<ActionResult<ApiResponse<KpisDto>>> ObtenerKpis(
        [FromQuery] ProyectoFiltroDto filtro)
    {
        var resultado = await _proyectoService.ObtenerKpisAsync(filtro);
        return Ok(resultado);
    }

    // GET api/proyectos/graficos/barras-apiladas
    [HttpGet("graficos/barras-apiladas")]
    public async Task<ActionResult<ApiResponse<GraficoBarrasApiladasDto>>> ObtenerBarrasApiladas(
        [FromQuery] ProyectoFiltroDto filtro)
    {
        var resultado = await _proyectoService.ObtenerBarrasApiladasAsync(filtro);
        return Ok(resultado);
    }

    // GET api/proyectos/graficos/plan-vs-real
    [HttpGet("graficos/plan-vs-real")]
    public async Task<ActionResult<ApiResponse<GraficoPlanVsRealDto>>> ObtenerPlanVsReal(
        [FromQuery] ProyectoFiltroDto filtro)
    {
        var resultado = await _proyectoService.ObtenerPlanVsRealAsync(filtro);
        return Ok(resultado);
    }

    // GET api/proyectos/descargar
    [HttpGet("descargar")]
    public async Task<IActionResult> Descargar([FromQuery] ProyectoFiltroDto filtro)
    {
        var archivo = await _proyectoService.DescargarExcelAsync(filtro);
        return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "proyectos.xlsx");
    }
}
