using System.Collections.Concurrent;
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
    // ── Caché en memoria para progreso en tiempo real ────────────────────────
    // Clave: consolidacionId — vive mientras el proceso está corriendo.
    // ObtenerEstadoAsync lo consulta primero; al finalizar se serializa a FuentesJson en BD.
    private static readonly ConcurrentDictionary<int, List<FuenteEstadoDto>> _progressCache = new();

    // Limpia etiquetas HTML que pueden venir del Excel (ej: "Cliente S.A.<br> - 001")
    private static readonly System.Text.RegularExpressions.Regex _htmlTagRegex =
        new("<[^>]*>", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string LimpiarHtml(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : _htmlTagRegex.Replace(valor, string.Empty).Trim();

    private readonly ApplicationDbContext _db;
    private readonly IGR55Parser _gr55Parser;
    private readonly IHorasParser _horasParser;
    private readonly IPlaneacionParser _planeacionParser;
    private readonly ITipoCambioParser _tipoCambioParser;
    private readonly IMaestroReferenciasParser _maestroParser;
    private readonly IP26Parser _p26Parser;
    private readonly IConfiguration _config;
    private readonly ILogger<ConsolidacionService> _logger;

    public ConsolidacionService(
        ApplicationDbContext db,
        IGR55Parser gr55Parser,
        IHorasParser horasParser,
        IPlaneacionParser planeacionParser,
        ITipoCambioParser tipoCambioParser,
        IMaestroReferenciasParser maestroParser,
        IP26Parser p26Parser,
        IConfiguration config,
        ILogger<ConsolidacionService> logger)
    {
        _db            = db;
        _gr55Parser    = gr55Parser;
        _horasParser   = horasParser;
        _planeacionParser = planeacionParser;
        _tipoCambioParser = tipoCambioParser;
        _maestroParser = maestroParser;
        _p26Parser     = p26Parser;
        _config        = config;
        _logger        = logger;
    }

    // ── Plan P26 aggregation bucket (por Vertical × Año × Mes) ───────────────
    private record P26Bucket(decimal IngresoPlan, decimal CostoPlan);

    // Clasificación de Task del Arch.P26 → ingreso/costo (HUE-02)
    private static readonly HashSet<string> _p26TasksIngreso = new(StringComparer.OrdinalIgnoreCase)
        { "1.3.1 Service Revenue", "1.3.2 Product Revenue", "1.3.3 Product Margin", "2.5.1 Product Cost" };
    private static readonly HashSet<string> _p26TasksCosto = new(StringComparer.OrdinalIgnoreCase)
        { "2.1.1 Labor Cost", "2.2.1 Traveling Cost", "2.3.1 Infrastructure Cost", "2.4.1 Other Direct Cost" };

    // Mapeo Source.Name (P26) → Cód.Industria del maestro. La vertical final se
    // resuelve con el mismo industriaDict que usa Proyecto, garantizando que el
    // string de vertical coincida exactamente para la comparación (Bug/HU P26).
    private static readonly Dictionary<string, string> _p26SourceToCodIndustria = new(StringComparer.OrdinalIgnoreCase)
    {
        ["01BFS"]             = "Z01", // Banca y finanzas
        ["02Transportation"]  = "Z06", // Transporte
        ["03Industrial"]      = "Z08", // Industrial
        ["04Retail"]          = "Z03", // Retail
        ["05HighTech"]        = "Z05", // Tecnología
        ["06Healthcare"]      = "Z10", // H&l
        ["07CPG"]             = "Z02", // Consumo masivo
        ["08NaturalResources"]= "Z09", // Recursos Naturales
        ["09Hospitality"]     = "Z07", // Servicios entretenimiento
        ["10Government"]      = "Z04", // Gobierno
        ["11Other"]           = "Z00", // Otros
    };

    // "01BFS.xlsx" → "01BFS"; tolera mayúsculas/minúsculas y la extensión.
    private static string NormalizarSourceP26(string sourceName)
    {
        var s = (sourceName ?? string.Empty).Trim();
        var dot = s.LastIndexOf('.');
        return dot > 0 ? s[..dot] : s;
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
    // CrearLogAsync — crea el log con estado Procesando, inicializa caché y retorna Id
    // ════════════════════════════════════════════════════════════════════════
    public async Task<int> CrearLogAsync(string iniciadoPor)
    {
        var log = new ConsolidacionLog
        {
            FechaInicio    = DateTime.UtcNow,
            Estado         = EstadoConsolidacion.Procesando,
            IniciadoPor    = iniciadoPor,
            TotalRegistros = 6, // 6 parsers = unidad de progreso inicial
        };
        _db.ConsolidacionLogs.Add(log);
        await _db.SaveChangesAsync();

        // Inicializar caché con los 6 archivos en estado Pendiente desde el primer momento
        var fuentesIniciales = new List<FuenteEstadoDto>
        {
            new() { Archivo = "GR55",               Estado = "Pendiente", RegistrosProcesados = 0, TotalRegistros = 0 },
            new() { Archivo = "Horas",               Estado = "Pendiente", RegistrosProcesados = 0, TotalRegistros = 0 },
            new() { Archivo = "Planeacion",          Estado = "Pendiente", RegistrosProcesados = 0, TotalRegistros = 0 },
            new() { Archivo = "TipoCambio",          Estado = "Pendiente", RegistrosProcesados = 0, TotalRegistros = 0 },
            new() { Archivo = "MaestroReferencias",  Estado = "Pendiente", RegistrosProcesados = 0, TotalRegistros = 0 },
            new() { Archivo = "P26",                 Estado = "Pendiente", RegistrosProcesados = 0, TotalRegistros = 0 },
        };
        _progressCache[log.Id] = fuentesIniciales;

        _logger.LogInformation("ConsolidacionService: log {Id} creado con estado Procesando.", log.Id);
        return log.Id;
    }

    // ════════════════════════════════════════════════════════════════════════
    // IniciarConsolidacionAsync — corre en background; recibe el Id del log
    // ════════════════════════════════════════════════════════════════════════
    public async Task IniciarConsolidacionAsync(int consolidacionId)
    {
        var log = await _db.ConsolidacionLogs.FindAsync(consolidacionId);
        if (log is null)
        {
            _logger.LogError("ConsolidacionService: log {Id} no encontrado.", consolidacionId);
            return;
        }

        // Asegurar que la caché existe aunque CrearLogAsync haya sido llamado desde otro scope
        if (!_progressCache.ContainsKey(consolidacionId))
        {
            _progressCache[consolidacionId] = new List<FuenteEstadoDto>
            {
                new() { Archivo = "GR55",              Estado = "Pendiente" },
                new() { Archivo = "Horas",             Estado = "Pendiente" },
                new() { Archivo = "Planeacion",        Estado = "Pendiente" },
                new() { Archivo = "TipoCambio",        Estado = "Pendiente" },
                new() { Archivo = "MaestroReferencias",Estado = "Pendiente" },
                new() { Archivo = "P26",               Estado = "Pendiente" },
            };
        }

        var warnings = new List<string>();

        try
        {
            var rutaBase = _config["ConsolidacionArchivos:RutaBase"] ?? string.Empty;

            // ── Parsear las 6 fuentes EN PARALELO ────────────────────────────
            // Los parsers son síncronos (trabajo CPU/IO que devuelve Task.FromResult),
            // por lo que se ofrecen a hilos del pool con Task.Run para que corran
            // realmente en paralelo. Cada parser lee su propio archivo y solo actualiza
            // el caché de progreso (thread-safe); NINGUNO toca el DbContext.
            var tGr55  = Task.Run(() => ParsearArchivo(consolidacionId,
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:GR55"]               ?? "GR55.xlsx"),
                _gr55Parser.ParsearAsync, "GR55", warnings));
            var tHoras = Task.Run(() => ParsearArchivo(consolidacionId,
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:Horas"]              ?? "Horas.xlsx"),
                _horasParser.ParsearAsync, "Horas", warnings));
            var tPlan  = Task.Run(() => ParsearArchivo(consolidacionId,
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:Planeacion"]         ?? "Planeacion.xlsx"),
                _planeacionParser.ParsearAsync, "Planeacion", warnings));
            var tTdc   = Task.Run(() => ParsearArchivo(consolidacionId,
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:TipoCambio"]         ?? "TDC.xlsx"),
                _tipoCambioParser.ParsearAsync, "TipoCambio", warnings));
            var tMaest = Task.Run(() => ParsearArchivo(consolidacionId,
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:MaestroReferencias"] ?? "MaestroReferencias.xlsx"),
                _maestroParser.ParsearAsync, "MaestroReferencias", warnings));
            var tP26   = Task.Run(() => ParsearArchivo(consolidacionId,
                Path.Combine(rutaBase, _config["ConsolidacionArchivos:P26"]                 ?? "P26.xlsx"),
                _p26Parser.ParsearAsync, "P26", warnings));

            await Task.WhenAll(tGr55, tHoras, tPlan, tTdc, tMaest, tP26);

            var gr55Registros       = tGr55.Result;
            var horasRegistros      = tHoras.Result;
            var planeacionRegistros = tPlan.Result;
            var tdcRegistros        = tTdc.Result;
            var maestro             = tMaest.Result ?? new MaestroReferenciasDto();
            var p26Registros        = tP26.Result;

            // Parseo completado (6 fuentes). El detalle por fuente ya se reflejó en el
            // caché en vivo durante el WhenAll.
            log.RegistrosExitosos = 6;
            await _db.SaveChangesAsync();

            // ── Persistir tasas COP en TiposCambio ─────────────────────────
            await PersistirTiposCambioAsync(tdcRegistros ?? []);

            // Lookup local de tasas para normalizar Planeación COP → USD
            var tdcDict = (tdcRegistros ?? [])
                .Where(r => r.TasaCop > 0)
                .GroupBy(r => (r.Año, r.Mes))
                .ToDictionary(g => g.Key, g => g.First().TasaCop);

            // ── Lookups desde el Maestro de referencias ──────────────────────
            var cuentaClasif = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in maestro.AccountsGroups.Where(a => !string.IsNullOrWhiteSpace(a.LineItemId)))
                cuentaClasif.TryAdd(a.LineItemId.Trim(), a.Clasificacion.Trim());

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

            // Responsable: responsable_wbs → nombre completo (Bug 145)
            var responsableDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in maestro.Responsables.Where(r => !string.IsNullOrWhiteSpace(r.ResponsableWbs)))
                responsableDict.TryAdd(r.ResponsableWbs.Trim(), r.ResponsableName.Trim());

            // ── Agregar GR55 → IngresoReal / CostoReal ───────────────────────
            // Solo cuentas clasificadas en el Maestro (Accounts_Group) afectan el cálculo:
            //   "Ingreso" → IngresoReal · "costos" → CostoReal.
            // Las cuentas sin clasificación se ignoran para no inflar el costo (Bug 138).
            var gr55Agg = new Dictionary<ClaveProyecto, Gr55Bucket>();
            var cuentasSinClasif = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in (gr55Registros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.ElementoPEP)))
            {
                var clave = new ClaveProyecto(r.ElementoPEP.Trim(), r.Ejercicio, r.PeriodoContable);
                var valor = r.ValorMonedaLocalCeBe;
                cuentaClasif.TryGetValue(r.NumeroCuenta.Trim(), out var clasif);

                var prev = gr55Agg.GetValueOrDefault(clave)
                           ?? new Gr55Bucket(0m, 0m, string.Empty, string.Empty);

                var esIngreso = clasif?.StartsWith("Ingreso", StringComparison.OrdinalIgnoreCase) == true;
                var esCosto   = clasif?.StartsWith("costo",   StringComparison.OrdinalIgnoreCase) == true;
                if (!esIngreso && !esCosto && valor != 0m)
                    cuentasSinClasif.Add(r.NumeroCuenta.Trim());

                gr55Agg[clave] = new Gr55Bucket(
                    IngresoReal     : prev.IngresoReal + (esIngreso ? valor  : 0m),
                    CostoReal       : prev.CostoReal   + (esCosto   ? -valor : 0m),
                    SocReceptora    : r.SocReceptora,
                    CentroBeneficio : r.CentroBeneficio);
            }

            if (cuentasSinClasif.Count > 0)
                warnings.Add($"GR55: {cuentasSinClasif.Count} cuenta(s) sin clasificación en el Maestro se ignoraron " +
                             $"(no suman ingreso ni costo): {string.Join(", ", cuentasSinClasif.OrderBy(c => c).Take(20))}" +
                             (cuentasSinClasif.Count > 20 ? "…" : ""));

            // ── Agregar Planeación → IngresoPlaneado / CostoPlaneado (COP → USD) ─
            var ultimaTasaCop = tdcDict.Count > 0
                ? tdcDict.OrderByDescending(kv => kv.Key.Año).ThenByDescending(kv => kv.Key.Mes).First().Value
                : 1m;

            var planAgg = new Dictionary<ClaveProyecto, PlanBucket>();
            var periodosSinTasa = new HashSet<(int Año, int Mes)>();

            foreach (var r in (planeacionRegistros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.Proyecto)))
            {
                if (!tdcDict.TryGetValue((r.Año, r.Mes), out var tasaCop) || tasaCop <= 0)
                {
                    periodosSinTasa.Add((r.Año, r.Mes));
                    tasaCop = ultimaTasaCop;
                }

                var clave = new ClaveProyecto(r.Proyecto.Trim(), r.Año, r.Mes);
                var prev  = planAgg.GetValueOrDefault(clave)
                            ?? new PlanBucket(0m, 0m, string.Empty, string.Empty, string.Empty, string.Empty);

                planAgg[clave] = new PlanBucket(
                    IngresoPlaneado : prev.IngresoPlaneado + r.IngresoPrevistoEur / tasaCop,
                    CostoPlaneado   : prev.CostoPlaneado   + r.CostePrevistoEur   / tasaCop,
                    Cliente         : r.Cliente    ?? string.Empty,
                    Cebe            : r.Cebe       ?? string.Empty,
                    Industria       : r.Industria  ?? string.Empty,
                    Responsable     : r.ResponsableWbs ?? string.Empty);
            }

            foreach (var (año, mes) in periodosSinTasa.OrderBy(x => x.Año).ThenBy(x => x.Mes))
                warnings.Add($"Planeación {año}/{mes:D2}: sin tasa TDC — se usó última tasa disponible ({ultimaTasaCop:F2}) como proxy.");

            // Fallback de responsable por proyecto: cuando un período no tiene responsable
            // (sin registro de planeación), se reutiliza el responsable_wbs del mismo proyecto
            // en otro período disponible (Bug 149).
            var responsableWbsPorProyecto = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in planAgg)
                if (!string.IsNullOrWhiteSpace(kv.Value.Responsable))
                    responsableWbsPorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.Responsable);

            // ── Agregar Horas por (Proyecto, Año, Mes) ───────────────────────
            // HorasBucket preserva proyecto_sociedad_fi además de las horas acumuladas.
            // Es la fuente primaria de Sociedad/País; GR55 SocReceptora es el fallback.
            var horasAgg = new Dictionary<ClaveProyecto, HorasBucket>();

            foreach (var r in (horasRegistros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.Proyecto)))
            {
                var clave    = new ClaveProyecto(r.Proyecto.Trim(), r.Año, r.Mes);
                var existing = horasAgg.GetValueOrDefault(clave);
                // Horas se acumulan; sociedad: tomar la primera no vacía encontrada
                var sociedad = !string.IsNullOrWhiteSpace(r.Sociedad)
                    ? r.Sociedad
                    : existing?.Sociedad ?? string.Empty;
                horasAgg[clave] = new HorasBucket((existing?.Horas ?? 0m) + r.Horas, sociedad);
            }

            // Mapa canónico de sociedad: colapsa todas las variantes crudas (con/sin
            // código de país, sólo código) a un único valor por código (Bug 133).
            var sociedadCanonica = ConstruirSociedadCanonica(
                horasAgg.Values.Select(b => b.Sociedad)
                    .Concat(gr55Agg.Values.Select(g => g.SocReceptora)),
                sociedadDict);

            // Fallback de metadatos por proyecto entre períodos (Bug 149): cuando un
            // período carece de cliente/industria/cebe/sociedad (porque la fuente que
            // los aporta no tiene ese año/mes), se reutiliza el primer valor no vacío
            // del mismo proyecto en cualquier otro período disponible.
            var clientePorProyecto   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var industriaPorProyecto = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var cebePorProyecto      = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var socRawPorProyecto    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in planAgg)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value.Cliente))   clientePorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.Cliente);
                if (!string.IsNullOrWhiteSpace(kv.Value.Industria)) industriaPorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.Industria);
                if (!string.IsNullOrWhiteSpace(kv.Value.Cebe))      cebePorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.Cebe);
            }
            foreach (var kv in horasAgg) // sociedad primaria = proyecto_sociedad_fi (Horas)
                if (!string.IsNullOrWhiteSpace(kv.Value.Sociedad)) socRawPorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.Sociedad);
            foreach (var kv in gr55Agg)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value.CentroBeneficio)) cebePorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.CentroBeneficio);
                if (!string.IsNullOrWhiteSpace(kv.Value.SocReceptora))    socRawPorProyecto.TryAdd(kv.Key.CodProyecto, kv.Value.SocReceptora);
            }

            // ── Unión de todas las claves únicas ─────────────────────────────
            var todasLasClaves = gr55Agg.Keys
                .Union(planAgg.Keys)
                .Union(horasAgg.Keys)
                .ToHashSet();

            // ── Crear entidades Proyecto ──────────────────────────────────────
            int exitosos = 0, fallidos = 0;
            var proyectos = new List<Proyecto>(todasLasClaves.Count);

            foreach (var clave in todasLasClaves)
            {
                try
                {
                    gr55Agg.TryGetValue(clave, out var g);
                    planAgg.TryGetValue(clave, out var p);
                    horasAgg.TryGetValue(clave, out var bucket);
                    var horas = bucket?.Horas ?? 0m;

                    // Sociedad: primaria = Horas (proyecto_sociedad_fi); fallback = GR55 SocReceptora;
                    // y, si el período no la tiene, el mismo proyecto en otro período (Bug 149).
                    // Se normaliza a un único valor canónico por sociedad (Bug 133).
                    var rawSoc = !string.IsNullOrWhiteSpace(bucket?.Sociedad)
                        ? bucket.Sociedad.Trim()
                        : !string.IsNullOrWhiteSpace(g?.SocReceptora)
                            ? g.SocReceptora.Trim()
                            : socRawPorProyecto.GetValueOrDefault(clave.CodProyecto, string.Empty).Trim();
                    string sociedad, pais;
                    if (sociedadCanonica.TryGetValue(rawSoc, out var canon))
                        { sociedad = canon.Nombre; pais = canon.Pais; }
                    else
                        { sociedad = rawSoc; pais = string.Empty; }

                    // CeBe: GR55 → Planeación → mismo proyecto en otro período (Bug 149)
                    var rawCebe = !string.IsNullOrWhiteSpace(g?.CentroBeneficio)
                        ? g.CentroBeneficio
                        : !string.IsNullOrWhiteSpace(p?.Cebe)
                            ? p!.Cebe
                            : cebePorProyecto.GetValueOrDefault(clave.CodProyecto, string.Empty);

                    var cebeNombre = rawCebe;
                    if (!string.IsNullOrWhiteSpace(rawCebe) && cebeDict.TryGetValue(rawCebe, out var cebeRef))
                        cebeNombre = cebeRef.Nombre;

                    // Industria: del período o, si falta, del mismo proyecto en otro período (Bug 149)
                    var industria = !string.IsNullOrWhiteSpace(p?.Industria)
                        ? p!.Industria
                        : industriaPorProyecto.GetValueOrDefault(clave.CodProyecto, string.Empty);
                    var vertical  = industriaDict.TryGetValue(industria, out var vNombre) ? vNombre : industria;

                    var area = string.Empty;
                    if (!string.IsNullOrWhiteSpace(rawCebe))
                        areaDict.TryGetValue(rawCebe, out area!);

                    // Responsable: wbs del período o, si falta, el del mismo proyecto en otro
                    // período (Bug 149); luego se traduce wbs → nombre completo (Bug 145).
                    var respWbs = !string.IsNullOrWhiteSpace(p?.Responsable)
                        ? p!.Responsable
                        : responsableWbsPorProyecto.GetValueOrDefault(clave.CodProyecto, string.Empty);
                    var responsable = !string.IsNullOrWhiteSpace(respWbs) && responsableDict.TryGetValue(respWbs.Trim(), out var respNombre)
                        ? respNombre
                        : LimpiarHtml(respWbs);

                    // Cliente: del período o, si falta, del mismo proyecto en otro período (Bug 149)
                    var cliente = !string.IsNullOrWhiteSpace(p?.Cliente)
                        ? p!.Cliente
                        : clientePorProyecto.GetValueOrDefault(clave.CodProyecto, string.Empty);

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
                        Cliente          = LimpiarHtml(cliente),
                        Responsable      = responsable,
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

            // ── Agregar plan P26 por Vertical × Año × Mes (HUE-02) ─────────────
            // Baseline de comparación del dashboard. Se clasifica el Amount por la
            // categoría del campo Task y se resuelve la vertical con el mismo
            // industriaDict que usa Proyecto (para que el string coincida exacto).
            var p26Agg = new Dictionary<(string Vertical, int Año, int Mes), P26Bucket>();
            var p26SinMapear = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in (p26Registros ?? []).Where(r => !string.IsNullOrWhiteSpace(r.SourceName)))
            {
                var esIngreso = _p26TasksIngreso.Contains(r.Task);
                var esCosto   = _p26TasksCosto.Contains(r.Task);
                if (!esIngreso && !esCosto) continue; // tasks de P&L no aplican al dashboard

                var src = NormalizarSourceP26(r.SourceName);
                if (!_p26SourceToCodIndustria.TryGetValue(src, out var codInd))
                { p26SinMapear.Add(src); continue; }
                var vertical = industriaDict.TryGetValue(codInd, out var v) ? v : codInd;

                // Amount viene en COP. Se guarda como "USD-equivalente" (Amount/tasaCop)
                // igual que IngresoPlaneado, para que el Factor del dashboard lo convierta
                // correctamente a COP o USD según el filtro de moneda.
                if (!tdcDict.TryGetValue((r.Año, r.Mes), out var tasaP26) || tasaP26 <= 0)
                    tasaP26 = ultimaTasaCop;

                var clave = (vertical, r.Año, r.Mes);
                var prev  = p26Agg.GetValueOrDefault(clave) ?? new P26Bucket(0m, 0m);
                p26Agg[clave] = new P26Bucket(
                    IngresoPlan: prev.IngresoPlan + (esIngreso ? r.Amount / tasaP26 : 0m),
                    CostoPlan  : prev.CostoPlan   + (esCosto   ? r.Amount / tasaP26 : 0m));
            }
            if (p26SinMapear.Count > 0)
                warnings.Add($"P26: {p26SinMapear.Count} Source.Name sin mapeo a vertical: {string.Join(", ", p26SinMapear.OrderBy(s => s))}");

            var planesP26 = p26Agg.Select(kv => new PlanVerticalP26
            {
                ConsolidacionId = log.Id,
                Vertical    = kv.Key.Vertical,
                Año         = kv.Key.Año,
                Mes         = kv.Key.Mes,
                IngresoPlan = kv.Value.IngresoPlan,
                CostoPlan   = kv.Value.CostoPlan,
            }).ToList();
            _db.PlanesVerticalP26.AddRange(planesP26);

            // ── Estado final y contadores reales ──────────────────────────────
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
            log.Errores           = warnings.Count > 0 ? JsonSerializer.Serialize(warnings) : null;

            // Persistir fuentes en BD y limpiar caché
            if (_progressCache.TryGetValue(consolidacionId, out var fuentesFinales))
                log.FuentesJson = JsonSerializer.Serialize(fuentesFinales);
            _progressCache.TryRemove(consolidacionId, out _);

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Consolidación {Id} completada: {Exitosos} proyectos, {Fallidos} errores, estado={Estado}",
                log.Id, exitosos, fallidos, estado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConsolidacionService: error fatal en consolidación {Id}", consolidacionId);

            var logFatal = await _db.ConsolidacionLogs.FindAsync(consolidacionId);
            if (logFatal is not null)
            {
                logFatal.FechaFin = DateTime.UtcNow;
                logFatal.Estado   = EstadoConsolidacion.Fallido;
                logFatal.Errores  = JsonSerializer.Serialize(new[] { ex.Message });

                // Guardar fuentes con error y limpiar caché
                if (_progressCache.TryGetValue(consolidacionId, out var fuentesError))
                    logFatal.FuentesJson = JsonSerializer.Serialize(fuentesError);
                _progressCache.TryRemove(consolidacionId, out _);

                await _db.SaveChangesAsync();
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // ObtenerEstadoAsync — lee caché primero (en vivo), luego BD (completado)
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

        // Avance en vivo: durante el parseo paralelo el porcentaje se deriva de las
        // fuentes ya completadas en el caché (no del contador secuencial). Al finalizar
        // (caché removido) se reporta 100% si la corrida terminó.
        int porcentaje;
        if (_progressCache.TryGetValue(consolidacionId, out var fuentesPct))
        {
            int totalF, hechasF;
            lock (fuentesPct)
            {
                totalF  = fuentesPct.Count;
                hechasF = fuentesPct.Count(f => f.Estado is "Exitoso" or "Fallido");
            }
            porcentaje = totalF > 0 ? (int)Math.Round(hechasF * 100.0 / totalF) : 0;
        }
        else
        {
            porcentaje = log.FechaFin.HasValue ? 100 : 0;
        }

        // ── Fuentes: caché en vivo (Procesando) → BD serializada (Completado) ─
        List<FuenteEstadoDto> fuentes = [];

        if (_progressCache.TryGetValue(consolidacionId, out var fuentesVivas))
        {
            // Copia thread-safe para serialización
            fuentes = fuentesVivas.Select(f => new FuenteEstadoDto
            {
                Archivo             = f.Archivo,
                Estado              = f.Estado,
                RegistrosProcesados = f.RegistrosProcesados,
                TotalRegistros      = f.TotalRegistros,
                Error               = f.Error,
            }).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(log.FuentesJson))
        {
            try   { fuentes = JsonSerializer.Deserialize<List<FuenteEstadoDto>>(log.FuentesJson) ?? []; }
            catch { /* ignorar deserialización fallida */ }
        }

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
            Fuentes           = fuentes,
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
    // Helper: persistir TiposCambio
    // ════════════════════════════════════════════════════════════════════════
    private async Task PersistirTiposCambioAsync(List<RegistroTipoCambioDto> registros)
    {
        if (registros.Count == 0) return;

        var existentes = await _db.TiposCambio
            .Where(t => t.Moneda == "COP" || t.Moneda == "USD")
            .ToDictionaryAsync(t => (t.Año, t.Mes, t.Moneda));

        foreach (var r in registros.Where(r => r.TasaCop > 0))
        {
            if (existentes.TryGetValue((r.Año, r.Mes, "COP"), out var cop))
                cop.Tasa = r.TasaCop;
            else
                _db.TiposCambio.Add(new Domain.Entities.TipoCambio
                    { Año = r.Año, Mes = r.Mes, Moneda = "COP", Tasa = r.TasaCop });

            if (!existentes.ContainsKey((r.Año, r.Mes, "USD")))
                _db.TiposCambio.Add(new Domain.Entities.TipoCambio
                    { Año = r.Año, Mes = r.Mes, Moneda = "USD", Tasa = 1m });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("ConsolidacionService: TiposCambio persistidos para {N} períodos.", registros.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helper: parsear un archivo con progreso en tiempo real en caché
    // ════════════════════════════════════════════════════════════════════════
    private async Task<T?> ParsearArchivo<T>(
        int consolidacionId,
        string ruta,
        Func<Stream, Action<int>?, Task<T>> parser,
        string nombre,
        List<string> warnings) where T : class
    {
        // Marcar como Procesando en caché (visible de inmediato al polling)
        ActualizarFuenteEnCache(consolidacionId, nombre, "Procesando", 0, 0, null);

        if (!File.Exists(ruta))
        {
            var error = $"Archivo no encontrado: {ruta}";
            ActualizarFuenteEnCache(consolidacionId, nombre, "Fallido", 0, 0, error);
            lock (warnings) warnings.Add($"{nombre}: {error}"); // thread-safe (parseo en paralelo)
            _logger.LogWarning("ConsolidacionService: {Msg}", error);
            return null;
        }

        try
        {
            // Callback llamado cada 100 filas por el parser → actualiza caché en tiempo real
            Action<int> onProgress = count =>
                ActualizarFuenteEnCache(consolidacionId, nombre, "Procesando", count, 0, null);

            await using var stream = File.OpenRead(ruta);
            var result = await parser(stream, onProgress);

            var total = result is System.Collections.ICollection col ? col.Count : 0;
            ActualizarFuenteEnCache(consolidacionId, nombre, "Exitoso", total, total, null);
            return result;
        }
        catch (Exception ex)
        {
            ActualizarFuenteEnCache(consolidacionId, nombre, "Fallido", 0, 0, ex.Message);
            lock (warnings) warnings.Add($"{nombre}: {ex.Message}"); // thread-safe (parseo en paralelo)
            _logger.LogWarning(ex, "ConsolidacionService: error parseando {Nombre}", nombre);
            return null;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helper: actualizar una fuente específica en el caché (thread-safe)
    // ════════════════════════════════════════════════════════════════════════
    private static void ActualizarFuenteEnCache(
        int consolidacionId,
        string archivo,
        string estado,
        int registrosProcesados,
        int totalRegistros,
        string? error)
    {
        if (!_progressCache.TryGetValue(consolidacionId, out var fuentes)) return;

        lock (fuentes)
        {
            var fuente = fuentes.FirstOrDefault(f => f.Archivo == archivo);
            if (fuente is null) return;
            fuente.Estado              = estado;
            fuente.RegistrosProcesados = registrosProcesados;
            fuente.TotalRegistros      = totalRegistros;
            fuente.Error               = error;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helper: construye el mapa raw→canónico de sociedad (Bug 133).
    // Agrupa por código numérico líder; el canónico es "{código} - {RazonSocial}"
    // del maestro si existe, o el nombre más completo visto (sin prefijo de país)
    // para códigos ausentes del maestro. Colapsa así variantes como
    // "1060", "1060 - PER - Nearshore..." y "1060 - Nearshore..." en un solo valor.
    // ════════════════════════════════════════════════════════════════════════
    private static Dictionary<string, (string Nombre, string Pais)> ConstruirSociedadCanonica(
        IEnumerable<string> valoresCrudos,
        Dictionary<string, SociedadReferenciaDto> porCodigo)
    {
        static string CodigoDe(string s)
        {
            var m = System.Text.RegularExpressions.Regex.Match(s ?? "", @"^\s*(\d{3,})");
            return m.Success ? m.Groups[1].Value : string.Empty;
        }
        static string NombreDe(string s)
        {
            var t = System.Text.RegularExpressions.Regex.Replace(s ?? "", @"^\s*\d{3,}\s*-?\s*", "");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"^[A-Z]{2,4}\s*-\s*", ""); // ISO país
            return t.Trim();
        }

        var distintos = valoresCrudos
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Mejor nombre visto por código (el más largo, sin prefijo de país)
        var mejorNombre = new Dictionary<string, string>();
        foreach (var v in distintos)
        {
            var cod = CodigoDe(v);
            if (cod == "") continue;
            var nom = NombreDe(v);
            if (!mejorNombre.TryGetValue(cod, out var prev) || nom.Length > prev.Length)
                mejorNombre[cod] = nom;
        }

        // Canónico por código (maestro si existe, si no el mejor nombre observado)
        var canonPorCodigo = new Dictionary<string, (string Nombre, string Pais)>();
        foreach (var cod in mejorNombre.Keys)
        {
            if (porCodigo.TryGetValue(cod, out var refM))
                canonPorCodigo[cod] = ($"{cod} - {refM.RazonSocial.Trim()}", refM.Pais?.Trim() ?? "");
            else
            {
                var nom = mejorNombre[cod];
                canonPorCodigo[cod] = (nom.Length > 0 ? $"{cod} - {nom}" : cod, "");
            }
        }

        var map = new Dictionary<string, (string Nombre, string Pais)>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in distintos)
        {
            var cod = CodigoDe(v);
            if (cod != "" && canonPorCodigo.TryGetValue(cod, out var canon))
                map[v] = canon;
            else
            {
                // Sin código: intentar coincidencia exacta por RazonSocial en el maestro
                var refByName = porCodigo.Values.FirstOrDefault(s =>
                    string.Equals(s.RazonSocial?.Trim(), v, StringComparison.OrdinalIgnoreCase));
                map[v] = refByName != null
                    ? ($"{refByName.Sociedad.Trim()} - {refByName.RazonSocial.Trim()}", refByName.Pais?.Trim() ?? "")
                    : (v, string.Empty);
            }
        }
        return map;
    }
}

// Bucket auxiliar para acumular horas y preservar proyecto_sociedad_fi por (Proyecto, Año, Mes)
file record HorasBucket(decimal Horas, string Sociedad);
