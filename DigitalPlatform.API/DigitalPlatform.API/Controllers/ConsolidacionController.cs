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
    private readonly IServiceScopeFactory _scopeFactory;

    public ConsolidacionController(
        IConsolidacionService consolidacionService,
        IConfiguration config,
        IServiceScopeFactory scopeFactory)
    {
        _consolidacionService = consolidacionService;
        _config               = config;
        _scopeFactory         = scopeFactory;
    }

    // ── Helper: lanza consolidación en background con scope DI propio ────────
    // Necesario para que el DbContext no sea el del request (que se dispone al retornar).
    private void LanzarEnBackground(int consolidacionId)
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IConsolidacionService>();
            await svc.IniciarConsolidacionAsync(consolidacionId);
        });
    }

    // ── Helper: guarda un único archivo en disco ────────────────────────────
    private async Task<ActionResult<ApiResponse<object>>> SubirArchivoAsync(
        IFormFile? archivo, string nombre, string claveConfig, string nombreDefecto)
    {
        if (archivo is null)
            return BadRequest(ApiResponse<object>.Fail($"El archivo '{nombre}' es requerido."));

        if (!Path.GetExtension(archivo.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail($"'{nombre}' debe tener extensión .xlsx."));

        var rutaBase = _config["ConsolidacionArchivos:RutaBase"] ?? "C:\\Archivos\\Consolidacion\\";
        if (!Directory.Exists(rutaBase)) Directory.CreateDirectory(rutaBase);

        var destino = Path.Combine(rutaBase, _config[claveConfig] ?? nombreDefecto);
        try
        {
            await using var fs = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.Read);
            await archivo.CopyToAsync(fs);
        }
        catch (IOException)
        {
            return Conflict(ApiResponse<object>.Fail(
                $"'{nombre}' está en uso por una consolidación en progreso."));
        }

        return Ok(ApiResponse<object>.Ok(
            new { Archivo = nombre, Estado = "Subido" },
            $"Archivo '{nombre}' subido correctamente."));
    }

    // ── Endpoints individuales por archivo ───────────────────────────────────
    // El frontend los llama en paralelo y luego dispara POST /iniciar.
    // Ventaja: cada archivo sube de forma independiente → tiempo total = el del más pesado.

    // POST api/consolidacion/upload/gr55
    [HttpPost("upload/gr55")]
    [RequestSizeLimit(536_870_912)]
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public Task<ActionResult<ApiResponse<object>>> UploadGR55(IFormFile archivo)
        => SubirArchivoAsync(archivo, "gr55", "ConsolidacionArchivos:GR55", "GR55.xlsx");

    // POST api/consolidacion/upload/horas
    [HttpPost("upload/horas")]
    [RequestSizeLimit(536_870_912)]
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public Task<ActionResult<ApiResponse<object>>> UploadHoras(IFormFile archivo)
        => SubirArchivoAsync(archivo, "horas", "ConsolidacionArchivos:Horas", "Horas.xlsx");

    // POST api/consolidacion/upload/planeacion
    [HttpPost("upload/planeacion")]
    [RequestSizeLimit(536_870_912)]
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public Task<ActionResult<ApiResponse<object>>> UploadPlaneacion(IFormFile archivo)
        => SubirArchivoAsync(archivo, "planeacion", "ConsolidacionArchivos:Planeacion", "Planeacion.xlsx");

    // POST api/consolidacion/upload/tipocambio
    [HttpPost("upload/tipocambio")]
    [RequestSizeLimit(536_870_912)]
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public Task<ActionResult<ApiResponse<object>>> UploadTipoCambio(IFormFile archivo)
        => SubirArchivoAsync(archivo, "tipoCambio", "ConsolidacionArchivos:TipoCambio", "TDC.xlsx");

    // POST api/consolidacion/upload/maestroreferencias
    [HttpPost("upload/maestroreferencias")]
    [RequestSizeLimit(536_870_912)]
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public Task<ActionResult<ApiResponse<object>>> UploadMaestroReferencias(IFormFile archivo)
        => SubirArchivoAsync(archivo, "maestroReferencias", "ConsolidacionArchivos:MaestroReferencias", "MaestroReferencias.xlsx");

    // POST api/consolidacion/iniciar
    // Llamar después de que los 5 uploads hayan respondido 200.
    [HttpPost("iniciar")]
    public async Task<ActionResult<ApiResponse<object>>> Iniciar()
    {
        var consolidacionId = await _consolidacionService.CrearLogAsync("sistema");
        LanzarEnBackground(consolidacionId);

        return Ok(ApiResponse<object>.Ok(
            new { ConsolidacionId = consolidacionId, Estado = "Procesando" },
            "Consolidación iniciada. Consulta GET /api/consolidacion/{id}/estado para ver el progreso."));
    }

    // POST api/consolidacion/upload  ← mantenido por compatibilidad
    [HttpPost("upload")]
    [RequestSizeLimit(536_870_912)]
    [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
    public async Task<ActionResult<ApiResponse<object>>> Upload(
        IFormFile gr55, IFormFile horas, IFormFile planeacion,
        IFormFile tipoCambio, IFormFile maestroReferencias)
    {
        var faltantes = new List<string>();
        if (gr55               is null) faltantes.Add("gr55");
        if (horas              is null) faltantes.Add("horas");
        if (planeacion         is null) faltantes.Add("planeacion");
        if (tipoCambio         is null) faltantes.Add("tipoCambio");
        if (maestroReferencias is null) faltantes.Add("maestroReferencias");
        if (faltantes.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(
                $"Faltan archivos: {string.Join(", ", faltantes)}."));

        var archivos = new Dictionary<string, IFormFile>
        {
            ["gr55"] = gr55!, ["horas"] = horas!, ["planeacion"] = planeacion!,
            ["tipoCambio"] = tipoCambio!, ["maestroReferencias"] = maestroReferencias!,
        };
        var noXlsx = archivos
            .Where(kv => !Path.GetExtension(kv.Value.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key).ToList();
        if (noXlsx.Count > 0)
            return BadRequest(ApiResponse<object>.Fail(
                $"Sin extensión .xlsx: {string.Join(", ", noXlsx)}."));

        var rutaBase = _config["ConsolidacionArchivos:RutaBase"] ?? "C:\\Archivos\\Consolidacion\\";
        if (!Directory.Exists(rutaBase)) Directory.CreateDirectory(rutaBase);

        var mapaNombres = new Dictionary<string, string>
        {
            ["gr55"]               = _config["ConsolidacionArchivos:GR55"]               ?? "GR55.xlsx",
            ["horas"]              = _config["ConsolidacionArchivos:Horas"]              ?? "Horas.xlsx",
            ["planeacion"]         = _config["ConsolidacionArchivos:Planeacion"]         ?? "Planeacion.xlsx",
            ["tipoCambio"]         = _config["ConsolidacionArchivos:TipoCambio"]         ?? "TDC.xlsx",
            ["maestroReferencias"] = _config["ConsolidacionArchivos:MaestroReferencias"] ?? "MaestroReferencias.xlsx",
        };
        try
        {
            foreach (var (clave, archivo) in archivos)
            {
                var destino = Path.Combine(rutaBase, mapaNombres[clave]);
                await using var fs = new FileStream(destino, FileMode.Create, FileAccess.Write, FileShare.Read);
                await archivo.CopyToAsync(fs);
            }
        }
        catch (IOException)
        {
            return Conflict(ApiResponse<object>.Fail(
                "Uno o más archivos en uso por una consolidación en progreso."));
        }

        var consolidacionId = await _consolidacionService.CrearLogAsync("sistema");
        LanzarEnBackground(consolidacionId);

        return Ok(ApiResponse<object>.Ok(
            new { ConsolidacionId = consolidacionId, Estado = "Procesando" },
            "Consolidación iniciada."));
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
