using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Consolidacion;
using DigitalPlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DigitalPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsolidacionController : ControllerBase
{
    private readonly IConsolidacionService _consolidacionService;
    private readonly IConfiguration _config;

    public ConsolidacionController(IConsolidacionService consolidacionService, IConfiguration config)
    {
        _consolidacionService = consolidacionService;
        _config               = config;
    }

    // POST api/consolidacion/upload
    [HttpPost("upload")]
    [RequestSizeLimit(536_870_912)] // 512 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public async Task<ActionResult<ApiResponse<ConsolidacionIniciadaDto>>> Upload(
        IFormFile gr55,
        IFormFile horas,
        IFormFile planeacion,
        IFormFile tipoCambio,
        IFormFile maestroReferencias)
    {
        // ── 1. Validar que los 5 archivos llegaron ───────────────────────────
        var faltantes = new List<string>();
        if (gr55               is null) faltantes.Add("gr55");
        if (horas              is null) faltantes.Add("horas");
        if (planeacion         is null) faltantes.Add("planeacion");
        if (tipoCambio         is null) faltantes.Add("tipoCambio");
        if (maestroReferencias is null) faltantes.Add("maestroReferencias");

        if (faltantes.Count > 0)
            return BadRequest(ApiResponse<ConsolidacionIniciadaDto>.Fail(
                $"Faltan los siguientes archivos: {string.Join(", ", faltantes)}."));

        // ── 2. Validar extensión .xlsx en los 5 archivos ────────────────────
        var archivos = new Dictionary<string, IFormFile>
        {
            ["gr55"]               = gr55!,
            ["horas"]              = horas!,
            ["planeacion"]         = planeacion!,
            ["tipoCambio"]         = tipoCambio!,
            ["maestroReferencias"] = maestroReferencias!,
        };

        var noXlsx = archivos
            .Where(kv => !Path.GetExtension(kv.Value.FileName)
                              .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList();

        if (noXlsx.Count > 0)
            return BadRequest(ApiResponse<ConsolidacionIniciadaDto>.Fail(
                $"Los siguientes archivos no tienen extensión .xlsx: {string.Join(", ", noXlsx)}."));

        // ── 3. Obtener / crear directorio base ───────────────────────────────
        var rutaBase = _config["ConsolidacionArchivos:RutaBase"] ?? "C:\\Archivos\\Consolidacion\\";
        if (!Directory.Exists(rutaBase))
            Directory.CreateDirectory(rutaBase);

        // ── 4. Guardar cada archivo con el nombre que espera el servicio ─────
        var mapaNombres = new Dictionary<string, string>
        {
            ["gr55"]               = _config["ConsolidacionArchivos:GR55"]               ?? "GR55.xlsx",
            ["horas"]              = _config["ConsolidacionArchivos:Horas"]              ?? "Horas.xlsx",
            ["planeacion"]         = _config["ConsolidacionArchivos:Planeacion"]         ?? "Planeacion.xlsx",
            ["tipoCambio"]         = _config["ConsolidacionArchivos:TipoCambio"]         ?? "TDC.xlsx",
            ["maestroReferencias"] = _config["ConsolidacionArchivos:MaestroReferencias"] ?? "MaestroReferencias.xlsx",
        };

        foreach (var (clave, archivo) in archivos)
        {
            var destino = Path.Combine(rutaBase, mapaNombres[clave]);
            await using var fs = System.IO.File.Create(destino);
            await archivo.CopyToAsync(fs);
        }

        // ── 5. Iniciar consolidación y retornar resultado ────────────────────
        var resultado = await _consolidacionService.IniciarConsolidacionAsync("sistema");
        return Ok(resultado);
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
