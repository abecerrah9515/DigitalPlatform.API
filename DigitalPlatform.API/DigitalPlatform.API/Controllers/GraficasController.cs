using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Proyectos;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/graficas")]
public class GraficasController : ControllerBase
{
    private readonly IProyectoService _proyectoService;

    public GraficasController(IProyectoService proyectoService)
        => _proyectoService = proyectoService;

    // GET /api/graficas/barras-apiladas?agruparPor=industria
    [HttpGet("barras-apiladas")]
    public async Task<ActionResult<ApiResponse<BarrasApiladasResponseDto>>> BarrasApiladas(
        [FromQuery] ProyectoFiltros filtro,
        [FromQuery] string agruparPor = "industria")
    {
        var resultado = await _proyectoService.GraficaBarrasApiladasAsync(filtro, agruparPor);
        return Ok(resultado);
    }

    // GET /api/graficas/plan-vs-real
    [HttpGet("plan-vs-real")]
    public async Task<ActionResult<ApiResponse<PlanVsRealResponseDto>>> PlanVsReal(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.GraficaPlanVsRealAsync(filtro);
        return Ok(resultado);
    }

    // GET /api/graficas/tendencia
    [HttpGet("tendencia")]
    public async Task<ActionResult<ApiResponse<TendenciaResponseDto>>> Tendencia(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.GraficaTendenciaAsync(filtro);
        return Ok(resultado);
    }

    // GET /api/graficas/top-clientes-horas
    [HttpGet("top-clientes-horas")]
    public async Task<ActionResult<ApiResponse<TopClientesHorasResponseDto>>> TopClientesHoras(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.GraficaTopClientesHorasAsync(filtro);
        return Ok(resultado);
    }

    // GET /api/graficas/treemap-area
    [HttpGet("treemap-area")]
    public async Task<ActionResult<ApiResponse<TreemapAreaResponseDto>>> TreemapArea(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.GraficaTreemapAreaAsync(filtro);
        return Ok(resultado);
    }

    // GET /api/graficas/scatter-burbuja
    [HttpGet("scatter-burbuja")]
    public async Task<ActionResult<ApiResponse<ScatterBurbujaResponseDto>>> ScatterBurbuja(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.GraficaScatterBurbujaAsync(filtro);
        return Ok(resultado);
    }

    // GET /api/graficas/heatmap-gm
    [HttpGet("heatmap-gm")]
    public async Task<ActionResult<ApiResponse<HeatmapGmResponseDto>>> HeatmapGm(
        [FromQuery] ProyectoFiltros filtro)
    {
        var resultado = await _proyectoService.GraficaHeatmapGmAsync(filtro);
        return Ok(resultado);
    }
}
