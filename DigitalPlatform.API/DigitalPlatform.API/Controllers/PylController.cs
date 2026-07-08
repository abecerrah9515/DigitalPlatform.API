using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Pyl;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/pyl")]
public class PylController : ControllerBase
{
    private readonly IPnlService _pnlService;

    public PylController(IPnlService pnlService) => _pnlService = pnlService;

    // GET /api/pyl?año=2025&cliente=&proyecto=&vertical=&moneda=COP
    // Tabla P&L jerárquica: árbol de cuentas con valores mensuales (ENE–DIC) + ACUM.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PnlResponseDto>>> ObtenerPnl(
        [FromQuery] PnlFiltros filtro)
    {
        var resultado = await _pnlService.ObtenerPnlAsync(filtro);
        return Ok(resultado);
    }

    // GET /api/pyl/filtros — valores de Cliente/Proyecto/Vertical + años (HUE-07)
    [HttpGet("filtros")]
    public async Task<ActionResult<ApiResponse<PnlFiltrosDto>>> ObtenerFiltros(
        [FromQuery] PnlFiltros filtro)
    {
        var resultado = await _pnlService.ObtenerFiltrosAsync(filtro);
        return Ok(resultado);
    }
}
