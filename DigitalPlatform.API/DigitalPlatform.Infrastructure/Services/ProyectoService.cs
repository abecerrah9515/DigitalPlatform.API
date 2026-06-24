using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Proyectos;
using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Domain.Enums;
using DigitalPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace DigitalPlatform.Infrastructure.Services;

public class ProyectoService : IProyectoService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ProyectoService> _logger;

    public ProyectoService(ApplicationDbContext db, ILogger<ProyectoService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Inner record — fila materializada del query base (Task 16)
    // ════════════════════════════════════════════════════════════════════════
    private sealed record Flat(
        int     Año,
        int     Mes,
        string  Cliente,
        string  CodProyecto,
        string  Industria,
        string  Vertical,
        string  Area,
        string  Sociedad,
        string  Pais,
        string  CeBe,
        string  Responsable,
        decimal IngresoReal,
        decimal IngresoPlaneado,
        decimal CostoReal,
        decimal CostoPlaneado,
        decimal Horas,
        decimal Factor);

    // ════════════════════════════════════════════════════════════════════════
    // Query base Task 16 — filtros array + JOIN TiposCambio por (Año,Mes,Moneda)
    // ════════════════════════════════════════════════════════════════════════
    private async Task<(List<Flat> datos, bool hayDatos)> CargarDatosAsync(ProyectoFiltros f)
    {
        var estadosValidos = new[] { EstadoConsolidacion.Exitoso, EstadoConsolidacion.ParcialmenteExitoso };

        var ultimoId = await _db.ConsolidacionLogs
            .Where(l => estadosValidos.Contains(l.Estado))
            .OrderByDescending(l => l.FechaInicio)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (ultimoId is null)
        {
            _logger.LogWarning("ProyectoService: sin consolidación exitosa o parcialmente exitosa.");
            return ([], false);
        }

        var q = _db.Proyectos.Where(p => p.ConsolidacionId == ultimoId);

        // Aplicar filtros array — genera IN (...) en SQL
        if (f.Año?.Length > 0)         q = q.Where(p => f.Año.Contains(p.Año));
        if (f.Mes?.Length > 0)         q = q.Where(p => f.Mes.Contains(p.Mes));
        if (f.Cliente?.Length > 0)     q = q.Where(p => f.Cliente.Contains(p.Cliente));
        if (f.CodProyecto?.Length > 0) q = q.Where(p => f.CodProyecto.Contains(p.CodProyecto));
        if (f.Vertical?.Length > 0)    q = q.Where(p => f.Vertical.Contains(p.Vertical));
        if (f.Area?.Length > 0)        q = q.Where(p => f.Area.Contains(p.Area));
        // Filtro UI "Sociedad" (viaja en el campo Pais) — muestra/filtra el PAÍS de la
        // hoja Sociedad del maestro (Bug 133/122).
        if (f.Pais?.Length > 0)        q = q.Where(p => f.Pais.Contains(p.Pais));

        var moneda = (f.Moneda ?? "COP").ToUpperInvariant();

        // Último TDC COP disponible — proxy para períodos futuros sin tasa registrada.
        var ultimaTasaCop = moneda == "COP"
            ? await _db.TiposCambio
                .Where(t => t.Moneda == "COP")
                .OrderByDescending(t => t.Año).ThenByDescending(t => t.Mes)
                .Select(t => t.Tasa)
                .FirstOrDefaultAsync()
            : 1m;
        if (ultimaTasaCop == 0) ultimaTasaCop = 1m;

        // LEFT JOIN con TiposCambio COP para obtener la tasa de conversión.
        // Si moneda == "USD" los valores ya están en USD → Factor = 1 siempre.
        var raw = await (
            from p in q
            join tc in _db.TiposCambio.Where(t => t.Moneda == "COP")
                on new { p.Año, p.Mes } equals new { tc.Año, tc.Mes } into g
            from tc in g.DefaultIfEmpty()
            select new
            {
                p.Año, p.Mes, p.Cliente, p.CodProyecto, p.Industria, p.Vertical,
                p.Area, p.Sociedad, p.Pais, p.CeBe, p.Responsable,
                p.IngresoReal, p.IngresoPlaneado, p.CostoReal, p.CostoPlaneado, p.Horas,
                TasaCop = (decimal?)tc.Tasa   // null cuando no hay TDC para ese período
            }
        ).ToListAsync();

        // Deduplicar: TiposCambio puede tener varias filas por período (múltiples consolidaciones),
        // lo que hace que el LEFT JOIN multiplique las filas de Proyectos.
        // Agrupa por (CodProyecto, Año, Mes) y acumula los valores financieros.
        var datos = raw
            .GroupBy(x => new { x.CodProyecto, x.Año, x.Mes })
            .Select(g =>
            {
                var first = g.First();
                var tasa  = g.Select(x => x.TasaCop).FirstOrDefault(t => t.HasValue);
                return new Flat(
                    first.Año, first.Mes, first.Cliente, first.CodProyecto, first.Industria, first.Vertical,
                    first.Area, first.Sociedad, first.Pais, first.CeBe, first.Responsable,
                    IngresoReal    : g.Sum(x => x.IngresoReal),
                    IngresoPlaneado: g.Sum(x => x.IngresoPlaneado),
                    CostoReal      : g.Sum(x => x.CostoReal),
                    CostoPlaneado  : g.Sum(x => x.CostoPlaneado),
                    Horas          : g.Sum(x => x.Horas),
                    Factor         : moneda == "USD" ? 1m : (tasa ?? ultimaTasaCop)
                );
            })
            .ToList();

        return (datos, true);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Plan de referencia Arch.P26 por (Año, Mes) — baseline de comparación.
    // Filtra por la(s) vertical(es) del filtro (si hay) o suma todo el portafolio.
    // Devuelve los valores ya convertidos a la moneda activa (USD-equiv * Factor).
    // ════════════════════════════════════════════════════════════════════════
    private async Task<Dictionary<(int Año, int Mes), (decimal Ingreso, decimal Costo)>>
        CargarPlanP26PorPeriodoAsync(ProyectoFiltros f)
    {
        var result = new Dictionary<(int Año, int Mes), (decimal, decimal)>();

        var estadosValidos = new[] { EstadoConsolidacion.Exitoso, EstadoConsolidacion.ParcialmenteExitoso };
        var ultimoId = await _db.ConsolidacionLogs
            .Where(l => estadosValidos.Contains(l.Estado))
            .OrderByDescending(l => l.FechaInicio)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
        if (ultimoId is null) return result;

        var q = _db.PlanesVerticalP26.Where(p => p.ConsolidacionId == ultimoId);
        if (f.Vertical?.Length > 0) q = q.Where(p => f.Vertical.Contains(p.Vertical));
        var planes = await q.ToListAsync();
        if (planes.Count == 0) return result;

        var moneda = (f.Moneda ?? "COP").ToUpperInvariant();

        // Factor de conversión por mes (igual que CargarDatosAsync): COP → tasaCop, USD → 1.
        var tasaMap = new Dictionary<(int, int), decimal>();
        var ultimaTasa = 1m;
        if (moneda == "COP")
        {
            var tasas = await _db.TiposCambio.Where(t => t.Moneda == "COP").ToListAsync();
            foreach (var t in tasas) tasaMap[(t.Año, t.Mes)] = t.Tasa;
            ultimaTasa = tasas.OrderByDescending(t => t.Año).ThenByDescending(t => t.Mes)
                              .Select(t => t.Tasa).FirstOrDefault();
            if (ultimaTasa == 0) ultimaTasa = 1m;
        }

        foreach (var g in planes.GroupBy(p => (p.Año, p.Mes)))
        {
            var factor = moneda == "USD"
                ? 1m
                : (tasaMap.TryGetValue(g.Key, out var t) && t > 0 ? t : ultimaTasa);
            result[g.Key] = (g.Sum(p => p.IngresoPlan) * factor, g.Sum(p => p.CostoPlan) * factor);
        }
        return result;
    }

    // Helpers de cálculo
    private static string Label(int año, int mes) => $"{año}-{mes:D2}";
    private static decimal Semaforo_Ingreso(decimal real, decimal plan) =>
        real >= plan ? 0m : 1m; // 0=Verde, 1=Rojo (helper numérico)

    // ── Período cerrado / valor efectivo (HUE-04, Bug 139) ───────────────────
    // Un período está cerrado si ya finalizó respecto a la fecha actual.
    // Para períodos cerrados se usa el valor REAL; para no cerrados, el PROYECTADO.
    private static bool EsPeriodoCerrado(int año, int mes)
    {
        var hoy = DateTime.Now;
        return año < hoy.Year || (año == hoy.Year && mes < hoy.Month);
    }
    private static decimal IngresoEfectivo(Flat d) =>
        EsPeriodoCerrado(d.Año, d.Mes) ? d.IngresoReal : d.IngresoPlaneado;
    private static decimal CostoEfectivo(Flat d) =>
        EsPeriodoCerrado(d.Año, d.Mes) ? d.CostoReal : d.CostoPlaneado;

    // ════════════════════════════════════════════════════════════════════════
    // GET /api/kpis — 5 indicadores (Task 16)
    // ════════════════════════════════════════════════════════════════════════
    private static readonly string[] _mesesAbr =
        ["Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic"];

    public async Task<ApiResponse<KpisDto>> ObtenerKpisAsync(ProyectoFiltros filtro)
    {
        // KPIs = YTD del año activo (HUE-04): cargar sin filtro temporal para determinar el período
        var filtroSinPeriodo = filtro with { Año = null, Mes = null };
        var (todosDatos, hayDatos) = await CargarDatosAsync(filtroSinPeriodo);
        if (!hayDatos || todosDatos.Count == 0)
            return ApiResponse<KpisDto>.Ok(new KpisDto(), "Sin datos disponibles.");

        // Año activo = filtro del usuario, o el año en curso (HUE-04: YTD del año activo)
        var añoActivo = filtro.Año?.Length > 0
            ? filtro.Año.Max()
            : DateTime.Now.Year;

        // Si no hay datos para el año en curso, caer al máximo disponible
        if (!todosDatos.Any(d => d.Año == añoActivo))
            añoActivo = todosDatos.Max(d => d.Año);

        // Mes activo = filtro del usuario, o el mes en curso (YTD hasta hoy).
        // Para años históricos sin mes explícito → mostrar el año completo (mes 12).
        var mesActivo = filtro.Mes?.Length > 0
            ? filtro.Mes.Max()
            : añoActivo < DateTime.Now.Year ? 12 : DateTime.Now.Month;

        // Selección de datos del KPI:
        //  - Con meses explícitos en el filtro → SOLO esos meses (alinea el GM% del KPI
        //    con scatter/heatmap/tabla, que respetan el filtro exacto — Bug 135).
        //  - Sin meses → acumulado YTD del año activo (mes 1 hasta mes activo, HUE-04).
        var datos = (filtro.Mes?.Length > 0
            ? todosDatos.Where(d => d.Año == añoActivo && filtro.Mes.Contains(d.Mes))
            : todosDatos.Where(d => d.Año == añoActivo && d.Mes <= mesActivo)).ToList();
        if (datos.Count == 0)
            return ApiResponse<KpisDto>.Ok(new KpisDto(), "Sin datos para el período activo.");

        // ── Métricas base ────────────────────────────────────────────────────
        // Ingreso/costo efectivos: real para meses cerrados, proyectado para no cerrados (Bug 139).
        var ingresoReal   = datos.Sum(d => IngresoEfectivo(d)  * d.Factor);
        var costoReal     = datos.Sum(d => CostoEfectivo(d)    * d.Factor);
        var horasTotal    = datos.Sum(d => d.Horas);
        var gm            = ingresoReal - costoReal;
        var gmPct         = ingresoReal != 0 ? gm / ingresoReal * 100m : 0m;
        var tarifa        = horasTotal  != 0 ? ingresoReal / horasTotal : 0m;

        // ── Comparación contra el plan de referencia Arch.P26 (HUG-03/HUE-04) ──
        // La comparación se deshabilita al filtrar por Cliente/Proyecto/Área, ya que
        // P26 está a nivel de vertical/portafolio (sin esa granularidad).
        var comparaAplica = !(filtro.Cliente?.Length > 0)
                         && !(filtro.CodProyecto?.Length > 0)
                         && !(filtro.Area?.Length > 0);

        var ingresoPlan = 0m;
        var costoPlan   = 0m;
        if (comparaAplica)
        {
            var mesesDatos = datos.Select(d => d.Mes).Distinct().ToHashSet();
            var planP26 = await CargarPlanP26PorPeriodoAsync(filtro);
            foreach (var kv in planP26.Where(kv => kv.Key.Año == añoActivo && mesesDatos.Contains(kv.Key.Mes)))
            {
                ingresoPlan += kv.Value.Ingreso;
                costoPlan   += kv.Value.Costo;
            }
        }

        var gmPlanPct     = ingresoPlan != 0 ? (ingresoPlan - costoPlan) / ingresoPlan * 100m : 0m;
        var gmDelta       = Math.Round(gmPct - gmPlanPct, 1);
        var cumplimiento  = ingresoPlan != 0 ? ingresoReal / ingresoPlan * 100m : 0m;
        var cumplDelta    = Math.Round(cumplimiento - 100m, 1);

        var monedaLabel = (filtro.Moneda ?? "COP").ToUpperInvariant();

        // ── Proyecto con más horas (badge HorasEntregadas) ───────────────────
        var proyMasHoras = datos
            .GroupBy(d => d.CodProyecto)
            .Select(g => (CodProyecto: g.Key, Horas: g.Sum(d => d.Horas)))
            .OrderByDescending(x => x.Horas)
            .FirstOrDefault();

        // ── Proyecto con tarifa más alta (badge TarifaEntregaPromedio) ───────
        var proyMayorTarifa = datos
            .GroupBy(d => d.CodProyecto)
            .Select(g =>
            {
                var h = g.Sum(d => d.Horas);
                return (CodProyecto: g.Key, Tarifa: h != 0 ? g.Sum(d => IngresoEfectivo(d) * d.Factor) / h : 0m);
            })
            .OrderByDescending(x => x.Tarifa)
            .FirstOrDefault();

        // ── Rango de meses para subtítulos ───────────────────────────────────
        var periodos   = datos.Select(d => (d.Año, d.Mes)).Distinct().OrderBy(x => x.Año).ThenBy(x => x.Mes).ToList();
        var primerPer  = periodos.First();
        var ultimoPer  = periodos.Last();
        var mesesEnAño = periodos.Select(p => p.Mes).Distinct().Count();
        var esCerrado  = mesesEnAño == 12;

        // Meses efectivamente considerados: los del filtro (si hay) o los presentes en datos
        var mesesFiltro = (filtro.Mes?.Length > 0
            ? filtro.Mes.OrderBy(m => m)
            : periodos.Select(p => p.Mes).Distinct().OrderBy(m => m)).ToArray();

        // Detectar si los meses son consecutivos (sin huecos)
        var esMesesConsecutivos = mesesFiltro.Length <= 1;
        if (!esMesesConsecutivos)
        {
            esMesesConsecutivos = true;
            for (int i = 1; i < mesesFiltro.Length; i++)
                if (mesesFiltro[i] - mesesFiltro[i - 1] != 1) { esMesesConsecutivos = false; break; }
        }

        string subtituloRango;
        if (esCerrado)
            subtituloRango = $"Todos {añoActivo}";
        else if (primerPer == ultimoPer)
            subtituloRango = $"{_mesesAbr[ultimoPer.Mes - 1]} {añoActivo}";
        else if (esMesesConsecutivos)
            // Rango continuo: "Ene–Abr 2026"
            subtituloRango = primerPer.Año == ultimoPer.Año
                ? $"{_mesesAbr[mesesFiltro[0] - 1]}–{_mesesAbr[mesesFiltro[^1] - 1]} {añoActivo}"
                : $"{_mesesAbr[mesesFiltro[0] - 1]} {primerPer.Año}–{_mesesAbr[mesesFiltro[^1] - 1]} {añoActivo}";
        else
            // Meses discretos no consecutivos: "Ene 2026, Mar 2026"
            subtituloRango = string.Join(", ", mesesFiltro.Select(m => $"{_mesesAbr[m - 1]} {añoActivo}"));

        var subtituloGM = $"{_mesesAbr[ultimoPer.Mes - 1]} {añoActivo} | {(esCerrado ? "Total" : añoActivo.ToString())}";

        return ApiResponse<KpisDto>.Ok(new KpisDto
        {
            IngresoTotalReal = new KpiItemDto
            {
                Valor      = Math.Round(ingresoReal, 2),
                Unidad     = monedaLabel,
                // Comparación contra P26; neutral si se filtró Cliente/Proyecto/Área.
                Semaforo   = !comparaAplica ? "Gris"   : ingresoReal >= ingresoPlan ? "Verde"  : "Rojo",
                Tendencia  = !comparaAplica ? "Neutro" : ingresoReal >= ingresoPlan ? "Arriba" : "Abajo",
                BadgeTexto = !comparaAplica ? "—"      : ingresoReal >= ingresoPlan ? "Sobre plan" : "Bajo plan",
                Subtitulo  = subtituloRango,
            },
            MargenGM = new KpiItemDto
            {
                Valor      = Math.Round(gmPct, 2),
                Unidad     = "%",
                // El semáforo de GM% es por umbral (no depende del plan); el diferencial sí.
                Semaforo   = gmPct >= 40 ? "Verde" : gmPct >= 35 ? "Amarillo" : "Rojo",
                Tendencia  = !comparaAplica ? "Neutro" : gmDelta >= 0 ? "Arriba" : "Abajo",
                BadgeTexto = !comparaAplica ? "—" : $"{(gmDelta >= 0 ? "▲" : "▼")} {Math.Abs(gmDelta)} pp vs plan",
                Subtitulo  = subtituloGM,
            },
            HorasEntregadas = new KpiItemDto
            {
                Valor      = Math.Round(horasTotal, 2),
                Unidad     = "h",
                Semaforo   = "Gris",
                Tendencia  = "Neutro",
                BadgeTexto = proyMasHoras.CodProyecto != default ? proyMasHoras.CodProyecto : "—",
                Subtitulo  = subtituloGM,
            },
            TarifaEntregaPromedio = new KpiItemDto
            {
                Valor      = Math.Round(tarifa, 2),
                Unidad     = monedaLabel,
                Semaforo   = "Gris",
                Tendencia  = "Neutro",
                BadgeTexto = proyMayorTarifa.CodProyecto != default ? proyMayorTarifa.CodProyecto : "—",
                Subtitulo  = subtituloGM,
            },
            CumplimientoIngresosPlan = new KpiItemDto
            {
                // Sin plan de referencia (filtro Cliente/Proyecto/Área) no hay cumplimiento.
                Valor      = !comparaAplica ? 0m : Math.Round(cumplimiento, 2),
                Unidad     = "%",
                Semaforo   = !comparaAplica ? "Gris"   : cumplimiento >= 100 ? "Verde"  : cumplimiento >= 90 ? "Amarillo" : "Rojo",
                Tendencia  = !comparaAplica ? "Neutro" : cumplimiento >= 100 ? "Arriba" : "Abajo",
                BadgeTexto = !comparaAplica ? "—" : $"{(cumplDelta >= 0 ? "▲ +" : "▼ ")}{cumplDelta}%",
                Subtitulo  = subtituloRango,
            },
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // GET /api/graficas/filtros/valores — filtros dependientes (HUE-03)
    // Cada dimensión se calcula aplicando todos los filtros activos EXCEPTO
    // el propio, produciendo cascada: seleccionar cliente limita proyectos, etc.
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<FiltrosValoresDto>> ObtenerFiltrosValoresAsync(ProyectoFiltros f)
    {
        var estadosValidos = new[] { EstadoConsolidacion.Exitoso, EstadoConsolidacion.ParcialmenteExitoso };

        var ultimoId = await _db.ConsolidacionLogs
            .Where(l => estadosValidos.Contains(l.Estado))
            .OrderByDescending(l => l.FechaInicio)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (ultimoId is null)
            return ApiResponse<FiltrosValoresDto>.Ok(new FiltrosValoresDto(), "Sin consolidación disponible.");

        var baseQ = _db.Proyectos
            .Where(p => p.ConsolidacionId == ultimoId)
            .AsNoTracking();

        // Aplica todos los filtros activos excepto la dimensión indicada.
        // Esto produce el comportamiento de filtros dependientes (cascada).
        IQueryable<Domain.Entities.Proyecto> Sin(string excluir)
        {
            var q = baseQ;
            if (excluir != "cliente"  && f.Cliente?.Length    > 0) q = q.Where(p => f.Cliente.Contains(p.Cliente));
            if (excluir != "proyecto" && f.CodProyecto?.Length > 0) q = q.Where(p => f.CodProyecto.Contains(p.CodProyecto));
            if (excluir != "vertical" && f.Vertical?.Length   > 0) q = q.Where(p => f.Vertical.Contains(p.Vertical));
            if (excluir != "area"     && f.Area?.Length        > 0) q = q.Where(p => f.Area.Contains(p.Area));
            // "pais" = filtro UI "Sociedad": filtra por el país del maestro (Bug 133/122)
            if (excluir != "pais"     && f.Pais?.Length        > 0) q = q.Where(p => f.Pais.Contains(p.Pais));
            if (excluir != "año"      && f.Año?.Length         > 0) q = q.Where(p => f.Año.Contains(p.Año));
            if (excluir != "mes"      && f.Mes?.Length         > 0) q = q.Where(p => f.Mes.Contains(p.Mes));
            return q;
        }

        // EF Core DbContext no es thread-safe → queries secuenciales
        var clientes   = await Sin("cliente").Select(p => p.Cliente).Where(v => v != "")
                             .Distinct().OrderBy(v => v).ToListAsync();
        var proyectos  = await Sin("proyecto").Select(p => p.CodProyecto).Where(v => v != "")
                             .Distinct().OrderBy(v => v).ToListAsync();
        var verticales = await Sin("vertical").Select(p => p.Vertical).Where(v => v != "")
                             .Distinct().OrderBy(v => v).ToListAsync();
        var areas      = await Sin("area").Select(p => p.Area).Where(v => v != "")
                             .Distinct().OrderBy(v => v).ToListAsync();
        // Dropdown "Sociedad" (campo Paises del DTO): listar el PAÍS de la hoja Sociedad
        // del maestro, que es lo que ahora muestra la columna en la tabla (Bug 133/122).
        var paises     = await Sin("pais").Select(p => p.Pais).Where(v => v != "")
                             .Distinct().OrderBy(v => v).ToListAsync();
        var años       = await Sin("año").Select(p => p.Año)
                             .Distinct().OrderBy(v => v).ToListAsync();
        var meses      = await Sin("mes").Select(p => p.Mes)
                             .Distinct().OrderBy(v => v).ToListAsync();

        return ApiResponse<FiltrosValoresDto>.Ok(new FiltrosValoresDto
        {
            Clientes   = clientes,
            Proyectos  = proyectos,
            Verticales = verticales,
            Areas      = areas,
            Paises     = paises,
            Años       = años,
            Meses      = meses,
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 1 — Barras apiladas por período × segmento
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<BarrasApiladasResponseDto>> GraficaBarrasApiladasAsync(
        ProyectoFiltros filtro, string agruparPor)
    {
        var (datos, _) = await CargarDatosAsync(filtro);
        var porPeriodo = datos
            .GroupBy(d => new { d.Año, d.Mes })
            .OrderBy(g => g.Key.Año).ThenBy(g => g.Key.Mes)
            .ToList();

        // Acumulado por (periodo, segmento) del periodo anterior para variación
        var ingPorPeriodoPrev = new Dictionary<(int, int), decimal>();
        var items             = new List<BarrasApiladasItemDto>();

        // Diccionario (año, mes, segmento) → ingreso para calcular variación por segmento
        var ingPorSegmentoPrev = new Dictionary<(int Año, int Mes, string Seg), decimal>();

        foreach (var periodo in porPeriodo)
        {
            // Valor efectivo: real en períodos cerrados, proyectado en no cerrados (Bug 139)
            var totalPeriodo = periodo.Sum(d => IngresoEfectivo(d) * d.Factor);
            var prevAño = periodo.Key.Mes == 1 ? periodo.Key.Año - 1 : periodo.Key.Año;
            var prevMes = periodo.Key.Mes == 1 ? 12 : periodo.Key.Mes - 1;

            var segmentos = periodo
                .GroupBy(d => agruparPor == "area" ? d.Area : d.Vertical)
                .Select(sg =>
                {
                    var ing = sg.Sum(d => IngresoEfectivo(d) * d.Factor);
                    ingPorSegmentoPrev.TryGetValue((prevAño, prevMes, sg.Key), out var ingSegPrev);
                    return new BarrasApiladasItemDto
                    {
                        Periodo                  = Label(periodo.Key.Año, periodo.Key.Mes),
                        Segmento                 = sg.Key,
                        Ingreso                  = Math.Round(ing, 2),
                        PorcentajeContribucion   = totalPeriodo != 0 ? Math.Round(ing / totalPeriodo * 100, 2) : 0m,
                        VariacionPeriodoAnterior = ingSegPrev != 0 ? Math.Round((ing - ingSegPrev) / ingSegPrev * 100, 2) : 0m,
                    };
                })
                .ToList();

            foreach (var item in segmentos)
                ingPorSegmentoPrev[(periodo.Key.Año, periodo.Key.Mes, item.Segmento)] = item.Ingreso;

            items.AddRange(segmentos);
        }

        return ApiResponse<BarrasApiladasResponseDto>.Ok(new BarrasApiladasResponseDto
        {
            AgrupadoPor = agruparPor,
            Items       = items,
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 2 — Plan vs Real (últimos 3 meses consecutivos)
    // El rango puede cruzar año (ej. Nov–Dic 2025 + Ene 2026).
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<PlanVsRealResponseDto>> GraficaPlanVsRealAsync(ProyectoFiltros filtro)
    {
        // Cargar sin restricción de año/mes para poder incluir meses del año anterior
        var filtroSinPeriodo = filtro with { Año = null, Mes = null };
        var (todosDatos, _) = await CargarDatosAsync(filtroSinPeriodo);
        if (todosDatos.Count == 0)
            return ApiResponse<PlanVsRealResponseDto>.Ok(new PlanVsRealResponseDto());

        // Período de referencia: mes/año seleccionado en filtro, o el más reciente con datos
        (int Año, int Mes) periodoRef;
        if (filtro.Año?.Length > 0 && filtro.Mes?.Length > 0)
        {
            periodoRef = (filtro.Año.Max(), filtro.Mes.Max());
        }
        else if (filtro.Año?.Length > 0)
        {
            int añoFiltro = filtro.Año.Max();
            int ultimoMes = todosDatos
                .Where(d => d.Año == añoFiltro && d.IngresoReal > 0)
                .Select(d => d.Mes)
                .DefaultIfEmpty(todosDatos.Where(d => d.Año == añoFiltro).Select(d => d.Mes).DefaultIfEmpty(0).Max())
                .Max();
            periodoRef = ultimoMes > 0 ? (añoFiltro, ultimoMes) : (añoFiltro, 12);
        }
        else
        {
            var refConReal = todosDatos
                .Where(d => d.IngresoReal > 0)
                .OrderByDescending(d => d.Año).ThenByDescending(d => d.Mes)
                .Select(d => (d.Año, d.Mes))
                .FirstOrDefault();
            periodoRef = refConReal != default
                ? refConReal
                : todosDatos.OrderByDescending(d => d.Año).ThenByDescending(d => d.Mes)
                            .Select(d => (d.Año, d.Mes)).First();
        }

        // 3 períodos consecutivos que terminan en periodoRef (representación lineal mes=año*12+mes-1)
        int refLinear = periodoRef.Año * 12 + (periodoRef.Mes - 1);
        var periodos3 = Enumerable.Range(0, 3)
            .Select(i => { int l = refLinear - (2 - i); return (Año: l / 12, Mes: l % 12 + 1); })
            .ToList();

        var datosFiltrados = todosDatos
            .Where(d => periodos3.Any(p => p.Año == d.Año && p.Mes == d.Mes));

        // "Plan" proviene de P26 (HUE-05). Se omite si se filtró Cliente/Proyecto/Área.
        var comparaAplica = !(filtro.Cliente?.Length > 0)
                         && !(filtro.CodProyecto?.Length > 0)
                         && !(filtro.Area?.Length > 0);
        var planP26 = comparaAplica
            ? await CargarPlanP26PorPeriodoAsync(filtro)
            : new Dictionary<(int Año, int Mes), (decimal Ingreso, decimal Costo)>();

        var realPorPeriodo = datosFiltrados
            .GroupBy(d => (d.Año, d.Mes))
            .ToDictionary(g => g.Key, g => g.Sum(d => d.IngresoReal * d.Factor));

        // Siempre 3 meses consecutivos (HUE-05), aunque algún mes no tenga real o plan.
        var periodos = periodos3
            .Select(p => new PlanVsRealPeriodoDto
            {
                Periodo         = Label(p.Año, p.Mes),
                IngresoPlaneado = Math.Round(planP26.TryGetValue((p.Año, p.Mes), out var pp) ? pp.Ingreso : 0m, 2),
                IngresoReal     = Math.Round(realPorPeriodo.TryGetValue((p.Año, p.Mes), out var r) ? r : 0m, 2),
            })
            .ToList();

        var tabla = periodos.Select(p =>
        {
            var variacion = p.IngresoPlaneado != 0
                ? (p.IngresoReal - p.IngresoPlaneado) / p.IngresoPlaneado * 100m
                : 0m;
            return new PlanVsRealTablaRowDto
            {
                Mes          = p.Periodo,
                Plan         = p.IngresoPlaneado,
                Real         = p.IngresoReal,
                VariacionPct = Math.Round(variacion, 2),
                Estado       = variacion >= 0 ? "Verde" : "Rojo",
            };
        }).ToList();

        return ApiResponse<PlanVsRealResponseDto>.Ok(new PlanVsRealResponseDto
        {
            Periodos     = periodos,
            TablaResumen = tabla,
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 3 — Tendencia (ingreso real vs planeado por período)
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<TendenciaResponseDto>> GraficaTendenciaAsync(ProyectoFiltros filtro)
    {
        var (datos, _) = await CargarDatosAsync(filtro);

        // Plan de referencia P26 (línea continua). Se omite si se filtró Cliente/Proyecto/Área.
        var comparaAplica = !(filtro.Cliente?.Length > 0)
                         && !(filtro.CodProyecto?.Length > 0)
                         && !(filtro.Area?.Length > 0);
        var planP26 = comparaAplica
            ? await CargarPlanP26PorPeriodoAsync(filtro)
            : new Dictionary<(int Año, int Mes), (decimal Ingreso, decimal Costo)>();

        var puntos = datos
            .GroupBy(d => new { d.Año, d.Mes })
            .OrderBy(g => g.Key.Año).ThenBy(g => g.Key.Mes)
            .Select(g =>
            {
                var real       = g.Sum(d => d.IngresoReal     * d.Factor); // real (GR55)
                var proyectado = g.Sum(d => d.IngresoPlaneado * d.Factor); // proyectado (Planeación)
                var plan       = planP26.TryGetValue((g.Key.Año, g.Key.Mes), out var pp) ? pp.Ingreso : 0m; // P26
                var sinPlan    = plan == 0;
                return new TendenciaPuntoDto
                {
                    Periodo         = Label(g.Key.Año, g.Key.Mes),
                    IngresoReal     = Math.Round(real, 2),
                    IngresoPlan     = Math.Round(plan, 2),
                    IngresoPlaneado = Math.Round(proyectado, 2),
                    SinPlan         = sinPlan,
                    Variacion       = sinPlan ? 0m : Math.Round((real - plan) / plan * 100m, 2),
                    PctCumplimiento = sinPlan ? 0m : Math.Round(real / plan * 100m, 2),
                };
            })
            .Where(p => p.IngresoReal != 0 || p.IngresoPlan != 0 || p.IngresoPlaneado != 0)
            .ToList();

        return ApiResponse<TendenciaResponseDto>.Ok(new TendenciaResponseDto { Puntos = puntos });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 4 — Top 10 clientes con más horas
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<TopClientesHorasResponseDto>> GraficaTopClientesHorasAsync(
        ProyectoFiltros filtro)
    {
        var (datos, _) = await CargarDatosAsync(filtro);
        var totalHoras = datos.Sum(d => d.Horas);

        var clientes = datos
            .GroupBy(d => d.Cliente)
            .Select(g =>
            {
                var horas = g.Sum(d => d.Horas);
                var areaMasHoras = g
                    .GroupBy(d => d.Area)
                    .OrderByDescending(ag => ag.Sum(d => d.Horas))
                    .Select(ag => ag.Key)
                    .FirstOrDefault() ?? string.Empty;
                return new { Cliente = g.Key, Horas = horas, AreaMasHoras = areaMasHoras };
            })
            .OrderByDescending(x => x.Horas)
            .Take(10)
            .Select(x => new ClienteHorasDto
            {
                Cliente          = x.Cliente,
                Horas            = Math.Round(x.Horas, 2),
                PctParticipacion = totalHoras != 0 ? Math.Round(x.Horas / totalHoras * 100, 2) : 0m,
                AreaMasHoras     = x.AreaMasHoras,
            })
            .ToList();

        return ApiResponse<TopClientesHorasResponseDto>.Ok(
            new TopClientesHorasResponseDto { Clientes = clientes });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 5 — Treemap de horas por área
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<TreemapAreaResponseDto>> GraficaTreemapAreaAsync(ProyectoFiltros filtro)
    {
        var (datos, _) = await CargarDatosAsync(filtro);
        var totalHoras = datos.Sum(d => d.Horas);

        var areas = datos
            .GroupBy(d => d.Area)
            .Select(g => new AreaHorasDto
            {
                Area              = g.Key,
                Horas             = Math.Round(g.Sum(d => d.Horas), 2),
                CantidadProyectos = g.Select(d => d.CodProyecto).Distinct().Count(),
                PctParticipacion  = totalHoras != 0
                    ? Math.Round(g.Sum(d => d.Horas) / totalHoras * 100, 2)
                    : 0m,
            })
            .OrderByDescending(a => a.Horas)
            .ToList();

        return ApiResponse<TreemapAreaResponseDto>.Ok(new TreemapAreaResponseDto { Areas = areas });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 6 — Scatter burbuja: Tarifa (X) vs GM% (Y), burbuja = Ingreso
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<ScatterBurbujaResponseDto>> GraficaScatterBurbujaAsync(
        ProyectoFiltros filtro)
    {
        var (datos, _) = await CargarDatosAsync(filtro);

        var clientes = datos
            .GroupBy(d => d.Cliente)
            .Select(g =>
            {
                var ing   = g.Sum(d => IngresoEfectivo(d) * d.Factor);
                var costo = g.Sum(d => CostoEfectivo(d)   * d.Factor);
                var horas = g.Sum(d => d.Horas);
                var gm    = ing != 0 ? (ing - costo) / ing * 100m : 0m;
                var tarifa = horas != 0 ? ing / horas : 0m;
                return new BurbujaClienteDto
                {
                    Cliente       = g.Key,
                    Area          = g.First().Area,
                    TarifaEntrega = Math.Round(tarifa, 2),
                    GmPct         = Math.Round(gm, 2),
                    Ingreso       = Math.Round(ing, 2),
                };
            })
            .OrderByDescending(c => c.Ingreso)
            .ToList();

        var totalIng   = clientes.Sum(c => c.Ingreso);
        var totalHoras = datos.Sum(d => d.Horas);
        var tarifaProm = totalHoras != 0 ? totalIng / totalHoras : 0m;

        return ApiResponse<ScatterBurbujaResponseDto>.Ok(new ScatterBurbujaResponseDto
        {
            Clientes      = clientes,
            TarifaPromedio = Math.Round(tarifaProm, 2),
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 7 — Heatmap GM% por cliente × mes (paginado: 10 clientes/página)
    // Clientes ordenados de menor a mayor GM% promedio (HUE-10)
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<HeatmapGmResponseDto>> GraficaHeatmapGmAsync(
        ProyectoFiltros filtro, int pagina = 1, int tamañoPagina = 10)
    {
        tamañoPagina = tamañoPagina is > 0 and <= 100 ? tamañoPagina : 10;
        var (datos, _) = await CargarDatosAsync(filtro);

        // Calcular GM% promedio por cliente para ordenar de menor a mayor (valor efectivo)
        var clientesOrdenados = datos
            .GroupBy(d => d.Cliente)
            .Select(g =>
            {
                var ing   = g.Sum(d => IngresoEfectivo(d) * d.Factor);
                var costo = g.Sum(d => CostoEfectivo(d)   * d.Factor);
                var gmProm = ing != 0 ? (ing - costo) / ing * 100m : 0m;
                return new { Cliente = g.Key, GmPromedio = gmProm };
            })
            .OrderBy(x => x.GmPromedio) // menor a mayor GM% promedio
            .ToList();

        var totalClientes = clientesOrdenados.Count;
        var paginaActual  = Math.Max(1, pagina);

        var clientesPagina = clientesOrdenados
            .Skip((paginaActual - 1) * tamañoPagina)
            .Take(tamañoPagina)
            .Select(x => x.Cliente)
            .ToHashSet();

        var celdas = datos
            .Where(d => clientesPagina.Contains(d.Cliente))
            .GroupBy(d => new { d.Cliente, d.Año, d.Mes })
            .Select(g =>
            {
                var ing   = g.Sum(d => IngresoEfectivo(d) * d.Factor);
                var costo = g.Sum(d => CostoEfectivo(d)   * d.Factor);
                var gm    = ing != 0 ? (ing - costo) / ing * 100m : 0m;
                return new HeatmapCeldaDto
                {
                    Cliente = g.Key.Cliente,
                    Periodo = Label(g.Key.Año, g.Key.Mes),
                    GmPct   = Math.Round(gm,   2),
                    Ingreso = Math.Round(ing,   2),
                    Costo   = Math.Round(costo, 2),
                };
            })
            // Mantener el orden GM% promedio dentro de la página, luego por período
            .OrderBy(c => clientesOrdenados.FindIndex(x => x.Cliente == c.Cliente))
            .ThenBy(c => c.Periodo)
            .ToList();

        return ApiResponse<HeatmapGmResponseDto>.Ok(new HeatmapGmResponseDto
        {
            Celdas       = celdas,
            TotalClientes = totalClientes,
            Pagina        = paginaActual,
            TamañoPagina  = tamañoPagina,
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Task 22 — Descarga Excel (HUE-11)
    // 15 columnas exactas del HUE-11, nombre: reporte_ejecutivo_{moneda}_{YYYY}_{MM}.xlsx
    // ════════════════════════════════════════════════════════════════════════
    public async Task<(byte[] Bytes, string NombreArchivo)> DescargarExcelFiltradoAsync(ProyectoFiltros f)
    {
        var (datos, _) = await CargarDatosAsync(f);

        var monedaLabel = (f.Moneda ?? "COP").ToUpperInvariant();

        var filas = datos.Select(d => new
        {
            Año              = d.Año,
            Mes              = d.Mes,
            Industria        = d.Industria,
            Cliente          = d.Cliente,
            CodProyecto      = d.CodProyecto,
            CeBe             = d.CeBe,
            Responsable      = d.Responsable,
            Area             = d.Area,
            // Sociedad = país del maestro (Bug 133/122); valores = valor efectivo (Bug 139)
            Sociedad         = d.Pais,
            Ingreso          = Math.Round(IngresoEfectivo(d) * d.Factor, 2),
            Costo            = Math.Round(CostoEfectivo(d)   * d.Factor, 2),
            GM               = Math.Round((IngresoEfectivo(d) - CostoEfectivo(d)) * d.Factor, 2),
            GMPct            = IngresoEfectivo(d) != 0
                                   ? Math.Round((IngresoEfectivo(d) - CostoEfectivo(d)) / IngresoEfectivo(d) * 100m, 2)
                                   : 0m,
            Horas            = d.Horas,
            TarifaEntrega    = d.Horas != 0
                                   ? Math.Round(IngresoEfectivo(d) * d.Factor / d.Horas, 2)
                                   : 0m,
        }).ToList();

        // Nombre de archivo según HUE-11 — derivado de los filtros activos, no del dataset
        var años  = f.Año?.Where(a => a > 0).OrderBy(a => a).ToArray() ?? [];
        var meses = f.Mes?.Where(m => m > 0).OrderBy(m => m).ToArray() ?? [];
        string nombreArchivo;
        if (años.Length == 0 && meses.Length == 0)
            nombreArchivo = $"reporte_ejecutivo_{monedaLabel}.xlsx";
        else if (años.Length == 1 && meses.Length == 1)
            nombreArchivo = $"reporte_ejecutivo_{monedaLabel}_{años[0]}_{meses[0]:D2}.xlsx";
        else if (años.Length == 1 && meses.Length == 0)
            nombreArchivo = $"reporte_ejecutivo_{monedaLabel}_{años[0]}.xlsx";
        else if (años.Length > 1 && meses.Length == 0)
            nombreArchivo = $"reporte_ejecutivo_{monedaLabel}_{años.First()}_{años.Last()}.xlsx";
        else if (años.Length == 1 && meses.Length > 1)
            nombreArchivo = $"reporte_ejecutivo_{monedaLabel}_{años[0]}_{meses.First():D2}_{meses.Last():D2}.xlsx";
        else
        {
            // Combinación múltiple años + meses: usar rango real del dataset
            var ultimo = datos.Count > 0
                ? datos.OrderByDescending(d => d.Año).ThenByDescending(d => d.Mes).First()
                : null;
            nombreArchivo = $"reporte_ejecutivo_{monedaLabel}_{ultimo?.Año ?? DateTime.UtcNow.Year}_{ultimo?.Mes ?? DateTime.UtcNow.Month:D2}.xlsx";
        }

        var stream = new MemoryStream();
        await stream.SaveAsAsync(filas);
        return (stream.ToArray(), nombreArchivo);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GET /api/proyectos — tabla paginada 15 columnas (Task 21)
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<PagedResult<ProyectoDto>>> ObtenerProyectosAsync(ProyectoFiltroDto filtro)
    {
        var f = new ProyectoFiltros
        {
            Moneda      = filtro.Moneda ?? "COP",
            Año         = filtro.Año?.Length > 0      ? filtro.Año                 : null,
            Mes         = filtro.Mes?.Length > 0      ? filtro.Mes                 : null,
            Cliente     = filtro.Cliente     != null  ? [filtro.Cliente]           : null,
            CodProyecto = filtro.CodProyecto != null  ? [filtro.CodProyecto]       : null,
            Vertical    = filtro.Industria   != null  ? [filtro.Industria]         : null,
            Area        = filtro.Area        != null  ? [filtro.Area]              : null,
            Pais        = filtro.Sociedad    != null  ? [filtro.Sociedad]          : null,
        };

        var (datos, hayDatos) = await CargarDatosAsync(f);
        if (!hayDatos)
            return ApiResponse<PagedResult<ProyectoDto>>.Ok(new PagedResult<ProyectoDto>(), "Sin datos disponibles.");

        // Solo mostrar filas con valor efectivo (ingreso/costo) distinto de cero.
        // Para períodos cerrados se evalúa el real; para no cerrados, el proyectado.
        // Filas sin actividad efectiva (ej. año histórico solo con horas) se excluyen —
        // el frontend muestra "Sin datos para esta selección".
        var datosFiltrados = datos
            .Where(d => IngresoEfectivo(d) != 0 || CostoEfectivo(d) != 0)
            .OrderByDescending(d => d.Año).ThenByDescending(d => d.Mes)
            .ToList();

        var pagina = Math.Max(1, filtro.Pagina);
        var tamaño = Math.Clamp(filtro.TamañoPagina, 1, 100);
        var total  = datosFiltrados.Count;

        var items = datosFiltrados
            .Skip((pagina - 1) * tamaño)
            .Take(tamaño)
            .Select(d => new ProyectoDto
            {
                Año           = d.Año,
                Mes           = d.Mes,
                Industria     = d.Industria,
                Cliente       = d.Cliente,
                CodProyecto   = d.CodProyecto,
                CeBe          = d.CeBe,
                Responsable   = d.Responsable,
                Area          = d.Area,
                // Columna "Sociedad" = país del maestro (hoja Sociedad, col País) — Bug 133/122
                Sociedad      = d.Pais,
                Ingreso       = Math.Round(IngresoEfectivo(d) * d.Factor, 2),
                Costo         = Math.Round(CostoEfectivo(d)   * d.Factor, 2),
                GM            = Math.Round((IngresoEfectivo(d) - CostoEfectivo(d)) * d.Factor, 2),
                GMPorcentaje  = IngresoEfectivo(d) != 0
                                    ? Math.Round((IngresoEfectivo(d) - CostoEfectivo(d)) / IngresoEfectivo(d) * 100m, 2)
                                    : 0m,
                Horas         = d.Horas,
                TarifaEntrega = d.Horas != 0
                                    ? Math.Round(IngresoEfectivo(d) * d.Factor / d.Horas, 2)
                                    : 0m,
            })
            .ToList();

        return ApiResponse<PagedResult<ProyectoDto>>.Ok(new PagedResult<ProyectoDto>
        {
            Items            = items,
            TotalRegistros   = total,
            Pagina           = pagina,
            TamañoPagina     = tamaño,
        });
    }

    public Task<ApiResponse<KpisDto>> ObtenerKpisAsync(ProyectoFiltroDto filtro)
        => Task.FromResult(ApiResponse<KpisDto>.Ok(new KpisDto(),
               "Usar GET /api/kpis con ProyectoFiltros (Task 16)."));

    public Task<ApiResponse<GraficoBarrasApiladasDto>> ObtenerBarrasApiladasAsync(ProyectoFiltroDto filtro)
        => Task.FromResult(ApiResponse<GraficoBarrasApiladasDto>.Ok(new GraficoBarrasApiladasDto()));

    public Task<ApiResponse<GraficoPlanVsRealDto>> ObtenerPlanVsRealAsync(ProyectoFiltroDto filtro)
        => Task.FromResult(ApiResponse<GraficoPlanVsRealDto>.Ok(new GraficoPlanVsRealDto()));

    public Task<byte[]> DescargarExcelAsync(ProyectoFiltroDto filtro)
    {
        _logger.LogWarning("DescargarExcelAsync: pendiente (Task 22).");
        return Task.FromResult(Array.Empty<byte>());
    }
}
