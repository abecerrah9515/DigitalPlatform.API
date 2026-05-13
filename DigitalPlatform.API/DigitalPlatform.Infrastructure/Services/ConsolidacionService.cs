using System.Text.Json;
using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Consolidacion;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Application.Interfaces.Parsers;
using DigitalPlatform.Domain.Entities;
using DigitalPlatform.Domain.Enums;
using DigitalPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Services;

public class ConsolidacionService : IConsolidacionService
{
    private readonly ApplicationDbContext _db;
    private readonly IGR55Parser _gr55Parser;
    private readonly IHorasParser _horasParser;
    private readonly IPlaneacionParser _planeacionParser;
    private readonly ITipoCambioParser _tipoCambioParser;
    private readonly IMaestroReferenciasParser _maestroParser;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsolidacionService> _logger;

    public ConsolidacionService(
        ApplicationDbContext db,
        IGR55Parser gr55Parser,
        IHorasParser horasParser,
        IPlaneacionParser planeacionParser,
        ITipoCambioParser tipoCambioParser,
        IMaestroReferenciasParser maestroParser,
        IConfiguration config,
        ILogger<ConsolidacionService> logger)
    {
        _db            = db;
        _gr55Parser    = gr55Parser;
        _horasParser   = horasParser;
        _planeacionParser = planeacionParser;
        _tipoCambioParser = tipoCambioParser;
        _maestroParser = maestroParser;
        _config        = config;
        _logger        = logger;
    }

    // ── Composite key shared across all aggregation dictionaries ────────────
    private record ClaveProyecto(string CodProyecto, int Año, int Mes);

    // ── GR55 aggregation bucket ──────────────────────────────────────────────
    private record Gr55Bucket(
        decimal IngresoReal,
        decimal CostoReal,
        string  SocReceptora,
        string  CentroBeneficio);

    // ── Planeación aggregation bucket ────────────────────────────────────────
    private record PlanBucket(
        decimal IngresoPlaneado,
        decimal CostoPlaneado,
        string  Cliente,
        string  Cebe,
        string  Industria,
        string  Responsable);

    // ════════════════════════════════════════════════════════════════════════
    // IniciarConsolidacionAsync
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<ConsolidacionIniciadaDto>> IniciarConsolidacionAsync(string iniciadoPor)
    {
        var warnings      = new List<string>();
        var fuentesEstado = new List<FuenteEstadoDto>();

        // ── Step 1: persist ConsolidacionLog para obtener el Id ─────────────
        var log = new ConsolidacionLog
        {
            FechaInicio = DateTime.UtcNow,
            Estado      = EstadoConsolidacion.Exitoso, // se actualiza al final
            IniciadoPor = iniciadoPor
        };
        _db.ConsolidacionLogs.Add(log);
        await _db.SaveChangesAsync();

        try
        {
            var rutaBase = _config["ConsolidacionArchivos:RutaBase"] ?? string.Empty;

            // ── Step 2: parsear los 5 archivos Excel ──────────────────────────
            var gr55Registros = await ParsearArchivo(
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:GR55"]               ?? "GR55.xlsx"),
                _gr55Parser.ParsearAsync, "GR55", fuentesEstado, warnings);

            var horasRegistros = await ParsearArchivo(
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:Horas"]              ?? "Horas.xlsx"),
                _horasParser.ParsearAsync, "Horas", fuentesEstado, warnings);

            var planeacionRegistros = await ParsearArchivo(
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:Planeacion"]         ?? "Planeacion.xlsx"),
                _planeacionParser.ParsearAsync, "Planeacion", fuentesEstado, warnings);

            var tdcRegistros = await ParsearArchivo(
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:TipoCambio"]         ?? "TDC.xlsx"),
                _tipoCambioParser.ParsearAsync, "TipoCambio", fuentesEstado, warnings);

            var maestro = await ParsearArchivo(
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:MaestroReferencias"] ?? "MaestroReferencias.xlsx"),
                _maestroParser.ParsearAsync, "MaestroReferencias", fuentesEstado, warnings)
                ?? new MaestroReferenciasDto();

            // ── Step 2b: persistir tasas COP y USD en TiposCambio ───────────
            await PersistirTiposCambioAsync(tdcRegistros ?? []);

            // ── Step 3: construir lookups desde el Maestro de referencias ────
            // TryAdd en todos los dicts para tolerar duplicados en el maestro (toma el primero)
            var cuentaClasif = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in maestro.AccountsGroups.Where(a => !string.IsNullOrWhiteSpace(a.Account)))
                cuentaClasif.TryAdd(a.Account.Trim(), a.Clasificacion.Trim());

            var cebeDict = new Dictionary<string, CeBeReferenciaDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in maestro.CeBes.Where(c => !string.IsNullOrWhiteSpace(c.CeBe)))
                cebeDict.TryAdd(c.CeBe.Trim(), c);

            var sociedadDict = new Dictionary<string, SociedadReferenciaDto>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in maestro.Sociedades.Where(s => !string.IsNullOrWhiteSpace(s.Sociedad)))
                sociedadDict.TryAdd(s.Sociedad.Trim(), s);

            var industriaDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in maestro.Industrias.Where(i => !string.IsNullOrWhiteSpace(i.CodIndustria)))
                industriaDict.TryAdd(i.CodIndustria.Trim(), i.Vertical.Trim());

            var areaDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in maestro.Areas.Where(a => !string.IsNullOrWhiteSpace(a.CeBe)))
                areaDict.TryAdd(a.CeBe.Trim(), a.Area.Trim());

            // ── Step 3b: persistir TipoCambio (COP y USD) ───────────────────
            await _db.TiposCambio.ExecuteDeleteAsync();
            var tiposCambio = new List<TipoCambio>();
            foreach (var tdc in tdcRegistros ?? [])
            {
                if (tdc.TasaCop != 0m)
                    tiposCambio.Add(new TipoCambio { Año = tdc.Año, Mes = tdc.Mes, Moneda = "COP", Tasa = tdc.TasaCop });
                if (tdc.TasaUsd != 0m)
                    tiposCambio.Add(new TipoCambio { Año = tdc.Año, Mes = tdc.Mes, Moneda = "USD", Tasa = tdc.TasaUsd });
            }
            _db.TiposCambio.AddRange(tiposCambio);

            // ── Step 4: agregar GR55 → IngresoReal / CostoReal ───────────────
            var gr55Agg = new Dictionary<ClaveProyecto, Gr55Bucket>();

            foreach (var r in (gr55Registros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.ElementoPEP)))
            {
                var clave = new ClaveProyecto(r.ElementoPEP.Trim(), r.Ejercicio, r.PeriodoContable);

                // Clasificar cuenta como Ingreso o Costo
                var valor = r.ValorMonedaLocalCeBe;
                cuentaClasif.TryGetValue(r.NumeroCuenta.Trim(), out var clasif);

                var prev = gr55Agg.GetValueOrDefault(clave)
                           ?? new Gr55Bucket(0m, 0m, string.Empty, string.Empty);

                var esIngreso = clasif?.Equals("Ingreso", StringComparison.OrdinalIgnoreCase) == true;

                gr55Agg[clave] = new Gr55Bucket(
                    IngresoReal     : prev.IngresoReal + (esIngreso ? valor : 0m),
                    CostoReal       : prev.CostoReal   + (esIngreso ? 0m : valor),
                    SocReceptora    : r.SocReceptora,
                    CentroBeneficio : r.CentroBeneficio);
            }

            // ── Step 5: agregar Planeación → IngresoPlaneado / CostoPlaneado ─
            var planAgg = new Dictionary<ClaveProyecto, PlanBucket>();

            foreach (var r in (planeacionRegistros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.Proyecto)))
            {
                var clave = new ClaveProyecto(r.Proyecto.Trim(), r.Año, r.Mes);
                var prev  = planAgg.GetValueOrDefault(clave)
                            ?? new PlanBucket(0m, 0m, string.Empty, string.Empty, string.Empty, string.Empty);

                planAgg[clave] = new PlanBucket(
                    IngresoPlaneado : prev.IngresoPlaneado + r.IngresoPrevistoEur,
                    CostoPlaneado   : prev.CostoPlaneado   + r.CostePrevistoEur,
                    Cliente         : r.Cliente    ?? string.Empty,
                    Cebe            : r.Cebe       ?? string.Empty,
                    Industria       : r.Industria  ?? string.Empty,
                    Responsable     : r.ResponsableWbs ?? string.Empty);
            }

            // ── Step 6: agregar Horas por (Proyecto, Año, Mes) ───────────────
            var horasAgg = new Dictionary<ClaveProyecto, decimal>();

            foreach (var r in (horasRegistros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.Proyecto)))
            {
                var clave = new ClaveProyecto(r.Proyecto.Trim(), r.Año, r.Mes);
                horasAgg[clave] = horasAgg.GetValueOrDefault(clave) + r.Horas;
            }

            // ── Step 7: unión de todas las claves únicas ─────────────────────
            var todasLasClaves = gr55Agg.Keys
                .Union(planAgg.Keys)
                .Union(horasAgg.Keys)
                .ToHashSet();

            // ── Step 8: crear entidades Proyecto ─────────────────────────────
            int exitosos = 0, fallidos = 0;
            var proyectos = new List<Proyecto>(todasLasClaves.Count);

            foreach (var clave in todasLasClaves)
            {
                try
                {
                    gr55Agg.TryGetValue(clave, out var g);
                    planAgg.TryGetValue(clave, out var p);
                    horasAgg.TryGetValue(clave, out var horas);

                    // Resolver Sociedad
                    var rawSoc = g?.SocReceptora ?? string.Empty;
                    sociedadDict.TryGetValue(rawSoc, out var socRef);
                    var sociedad = socRef?.RazonSocial ?? rawSoc;
                    var pais     = socRef?.Pais ?? string.Empty;

                    // Resolver CeBe: GR55 tiene prioridad; fallback a Planeación
                    var rawCebe = !string.IsNullOrWhiteSpace(g?.CentroBeneficio)
                        ? g.CentroBeneficio
                        : p?.Cebe ?? string.Empty;

                    var cebeNombre = rawCebe;
                    var cebeGroup  = string.Empty;
                    if (!string.IsNullOrWhiteSpace(rawCebe) && cebeDict.TryGetValue(rawCebe, out var cebeRef))
                    {
                        cebeNombre = cebeRef.Nombre;
                        cebeGroup  = cebeRef.CeBeGroup; // CeBeGroup IS el nombre display de la vertical
                    }

                    // Industria = CodIndustria de la fuente (e.g. "Z01")
                    var industria = p?.Industria ?? string.Empty;

                    // Vertical = nombre display (= CeBeGroup); fallback a Planeación.Industria
                    var vertical = !string.IsNullOrWhiteSpace(cebeGroup) ? cebeGroup : industria;

                    // Resolver Area via CeBe → Areas
                    var area = string.Empty;
                    if (!string.IsNullOrWhiteSpace(rawCebe))
                        areaDict.TryGetValue(rawCebe, out area!);

                    proyectos.Add(new Proyecto
                    {
                        ConsolidacionId  = log.Id,
                        CodProyecto      = clave.CodProyecto,
                        Año              = clave.Año,
                        Mes              = clave.Mes,
                        IngresoReal      = g?.IngresoReal      ?? 0m,
                        CostoReal        = g?.CostoReal        ?? 0m,
                        IngresoPlaneado  = p?.IngresoPlaneado  ?? 0m,
                        CostoPlaneado    = p?.CostoPlaneado    ?? 0m,
                        Horas            = horas,
                        Sociedad         = sociedad,
                        Pais             = pais,
                        CeBe             = cebeNombre,
                        Industria        = industria,
                        Vertical         = vertical ?? string.Empty,
                        Area             = area ?? string.Empty,
                        Cliente          = p?.Cliente    ?? string.Empty,
                        Responsable      = p?.Responsable ?? string.Empty,
                    });

                    exitosos++;
                }
                catch (Exception ex)
                {
                    fallidos++;
                    var msg = $"Error procesando {clave.CodProyecto} {clave.Año}/{clave.Mes}: {ex.Message}";
                    warnings.Add(msg);
                    _logger.LogWarning(ex, "ConsolidacionService: {Msg}", msg);
                }
            }

            _db.Proyectos.AddRange(proyectos);

            // ── Determinar estado final ───────────────────────────────────────
            var estado = exitosos == 0
                ? EstadoConsolidacion.Fallido
                : fallidos > 0 || warnings.Count > 0
                    ? EstadoConsolidacion.ParcialmenteExitoso
                    : EstadoConsolidacion.Exitoso;

            log.FechaFin          = DateTime.UtcNow;
            log.Estado            = estado;
            log.TotalRegistros    = exitosos + fallidos;
            log.RegistrosExitosos = exitosos;
            log.RegistrosFallidos = fallidos;
            log.Errores           = warnings.Count > 0
                ? JsonSerializer.Serialize(warnings)
                : null;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Consolidación {Id} completada: {Exitosos} proyectos, {Fallidos} errores, estado={Estado}",
                log.Id, exitosos, fallidos, estado);

            return ApiResponse<ConsolidacionIniciadaDto>.Ok(
                new ConsolidacionIniciadaDto
                {
                    ConsolidacionId = log.Id,
                    FechaInicio     = log.FechaInicio,
                    Estado          = log.Estado.ToString()
                },
                $"Consolidación completada: {exitosos} proyectos procesados.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConsolidacionService: error fatal en consolidación {Id}", log.Id);

            log.FechaFin = DateTime.UtcNow;
            log.Estado   = EstadoConsolidacion.Fallido;
            log.Errores  = JsonSerializer.Serialize(new[] { ex.Message });
            await _db.SaveChangesAsync();

            return ApiResponse<ConsolidacionIniciadaDto>.Fail($"Error fatal: {ex.Message}");
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // ObtenerEstadoAsync
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<ConsolidacionEstadoDto>> ObtenerEstadoAsync(int consolidacionId)
    {
        var log = await _db.ConsolidacionLogs.FindAsync(consolidacionId);
        if (log is null)
            return ApiResponse<ConsolidacionEstadoDto>.Fail($"Consolidación {consolidacionId} no encontrada.");

        List<string> errores = [];
        if (!string.IsNullOrWhiteSpace(log.Errores))
        {
            try   { errores = JsonSerializer.Deserialize<List<string>>(log.Errores) ?? []; }
            catch { errores = [log.Errores]; }
        }

        var porcentaje = log.TotalRegistros > 0
            ? (int)Math.Round(log.RegistrosExitosos * 100.0 / log.TotalRegistros)
            : log.FechaFin.HasValue ? 100 : 0;

        return ApiResponse<ConsolidacionEstadoDto>.Ok(new ConsolidacionEstadoDto
        {
            ConsolidacionId   = log.Id,
            Estado            = log.Estado.ToString(),
            PorcentajeAvance  = porcentaje,
            TotalRegistros    = log.TotalRegistros,
            RegistrosExitosos = log.RegistrosExitosos,
            RegistrosFallidos = log.RegistrosFallidos,
            FechaInicio       = log.FechaInicio,
            FechaFin          = log.FechaFin,
            Errores           = errores,
            Fuentes           = [],  // no se persisten fuentes individuales por consolidación
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // ObtenerHistorialAsync
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<PagedResult<ConsolidacionHistorialDto>>> ObtenerHistorialAsync(
        int pagina, int tamañoPagina)
    {
        tamañoPagina = Math.Clamp(tamañoPagina, 1, 100);
        pagina       = Math.Max(1, pagina);

        var query = _db.ConsolidacionLogs.OrderByDescending(l => l.FechaInicio);
        var total = await query.CountAsync();

        var items = await query
            .Skip((pagina - 1) * tamañoPagina)
            .Take(tamañoPagina)
            .Select(l => new ConsolidacionHistorialDto
            {
                Id                = l.Id,
                FechaInicio       = l.FechaInicio,
                FechaFin          = l.FechaFin,
                Estado            = l.Estado.ToString(),
                TotalRegistros    = l.TotalRegistros,
                RegistrosExitosos = l.RegistrosExitosos,
                RegistrosFallidos = l.RegistrosFallidos,
                IniciadoPor       = l.IniciadoPor
            })
            .ToListAsync();

        return ApiResponse<PagedResult<ConsolidacionHistorialDto>>.Ok(new PagedResult<ConsolidacionHistorialDto>
        {
            Items          = items,
            TotalRegistros = total,
            Pagina         = pagina,
            TamañoPagina   = tamañoPagina
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helper: persistir TiposCambio — COP (del TDC) + USD = 1 para cada período
    // ════════════════════════════════════════════════════════════════════════
    private async Task PersistirTiposCambioAsync(List<RegistroTipoCambioDto> registros)
    {
        if (registros.Count == 0) return;

        var existentes = await _db.TiposCambio
            .Where(t => t.Moneda == "COP" || t.Moneda == "USD")
            .ToDictionaryAsync(t => (t.Año, t.Mes, t.Moneda));

        foreach (var r in registros.Where(r => r.TasaCop > 0))
        {
            // Tasa COP (del TDC)
            if (existentes.TryGetValue((r.Año, r.Mes, "COP"), out var cop))
                cop.Tasa = r.TasaCop;
            else
                _db.TiposCambio.Add(new Domain.Entities.TipoCambio
                    { Año = r.Año, Mes = r.Mes, Moneda = "COP", Tasa = r.TasaCop });

            // Tasa USD = 1 (valores ya están en USD)
            if (!existentes.ContainsKey((r.Año, r.Mes, "USD")))
                _db.TiposCambio.Add(new Domain.Entities.TipoCambio
                    { Año = r.Año, Mes = r.Mes, Moneda = "USD", Tasa = 1m });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("ConsolidacionService: TiposCambio persistidos para {N} períodos.", registros.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helper: parsear un archivo Excel con manejo defensivo de errores
    // ════════════════════════════════════════════════════════════════════════
    private async Task<T?> ParsearArchivo<T>(
        string ruta,
        Func<Stream, Task<T>> parser,
        string nombre,
        List<FuenteEstadoDto> fuentesEstado,
        List<string> warnings) where T : class
    {
        var fuente = new FuenteEstadoDto { Archivo = nombre, Estado = "Pendiente" };
        fuentesEstado.Add(fuente);

        if (!File.Exists(ruta))
        {
            fuente.Estado = "Fallido";
            fuente.Error  = $"Archivo no encontrado: {ruta}";
            warnings.Add($"{nombre}: {fuente.Error}");
            _logger.LogWarning("ConsolidacionService: {Msg}", fuente.Error);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(ruta);
            var result = await parser(stream);

            fuente.Estado              = "Exitoso";
            fuente.RegistrosProcesados = result is System.Collections.ICollection col ? col.Count : 0;
            return result;
        }
        catch (Exception ex)
        {
            fuente.Estado = "Fallido";
            fuente.Error  = ex.Message;
            warnings.Add($"{nombre}: {ex.Message}");
            _logger.LogWarning(ex, "ConsolidacionService: error parseando {Nombre}", nombre);
            return null;
        }
    }
}
