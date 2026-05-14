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
        var ultimoId = await _db.ConsolidacionLogs
            .Where(l => l.Estado == EstadoConsolidacion.Exitoso)
            .OrderByDescending(l => l.FechaInicio)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (ultimoId is null)
        {
            _logger.LogWarning("ProyectoService: sin consolidación exitosa.");
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
        if (f.Pais?.Length > 0)        q = q.Where(p => f.Pais.Contains(p.Pais));

        var moneda = (f.Moneda ?? "COP").ToUpperInvariant();

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
                TasaCop = tc == null ? 1m : tc.Tasa
            }
        ).ToListAsync();

        var datos = raw.Select(x => new Flat(
            x.Año, x.Mes, x.Cliente, x.CodProyecto, x.Industria, x.Vertical,
            x.Area, x.Sociedad, x.Pais, x.CeBe, x.Responsable,
            x.IngresoReal, x.IngresoPlaneado, x.CostoReal, x.CostoPlaneado, x.Horas,
            Factor: moneda == "USD" ? 1m : x.TasaCop
        )).ToList();

        return (datos, true);
    }

    // Helpers de cálculo
    private static string Label(int año, int mes) => $"{año}-{mes:D2}";
    private static decimal Semaforo_Ingreso(decimal real, decimal plan) =>
        real >= plan ? 0m : 1m; // 0=Verde, 1=Rojo (helper numérico)

    // ════════════════════════════════════════════════════════════════════════
    // GET /api/kpis — 5 indicadores (Task 16)
    // ════════════════════════════════════════════════════════════════════════
    private static readonly string[] _mesesAbr =
        ["Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic"];

    public async Task<ApiResponse<KpisDto>> ObtenerKpisAsync(ProyectoFiltros filtro)
    {
        var (datos, hayDatos) = await CargarDatosAsync(filtro);
        if (!hayDatos || datos.Count == 0)
            return ApiResponse<KpisDto>.Ok(new KpisDto(), "Sin datos disponibles.");

        // ── Métricas base ────────────────────────────────────────────────────
        var ingresoReal   = datos.Sum(d => d.IngresoReal      * d.Factor);
        var ingresoPlan   = datos.Sum(d => d.IngresoPlaneado   * d.Factor);
        var costoReal     = datos.Sum(d => d.CostoReal         * d.Factor);
        var costoPlan     = datos.Sum(d => d.CostoPlaneado     * d.Factor);
        var horasTotal    = datos.Sum(d => d.Horas);
        var gm            = ingresoReal - costoReal;
        var gmPct         = ingresoReal != 0 ? gm / ingresoReal * 100m : 0m;
        var gmPlanPct     = ingresoPlan != 0 ? (ingresoPlan - costoPlan) / ingresoPlan * 100m : 0m;
        var gmDelta       = Math.Round(gmPct - gmPlanPct, 1);
        var tarifa        = horasTotal  != 0 ? ingresoReal / horasTotal : 0m;
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
                return (CodProyecto: g.Key, Tarifa: h != 0 ? g.Sum(d => d.IngresoReal * d.Factor) / h : 0m);
            })
            .OrderByDescending(x => x.Tarifa)
            .FirstOrDefault();

        // ── Rango de meses para subtítulos ───────────────────────────────────
        var periodos   = datos.Select(d => (d.Año, d.Mes)).Distinct().OrderBy(x => x.Año).ThenBy(x => x.Mes).ToList();
        var primerPer  = periodos.First();
        var ultimoPer  = periodos.Last();
        var añoActivo  = ultimoPer.Año;
        var mesesEnAño = periodos.Where(p => p.Año == añoActivo).Select(p => p.Mes).Distinct().Count();
        var esCerrado  = mesesEnAño == 12;

        var subtituloRango = esCerrado
            ? $"Todos {añoActivo}"
            : primerPer == ultimoPer
                ? $"{_mesesAbr[ultimoPer.Mes - 1]} {añoActivo}"
                : primerPer.Año == ultimoPer.Año
                    ? $"{_mesesAbr[primerPer.Mes - 1]}–{_mesesAbr[ultimoPer.Mes - 1]} {añoActivo}"
                    : $"{_mesesAbr[primerPer.Mes - 1]} {primerPer.Año}–{_mesesAbr[ultimoPer.Mes - 1]} {añoActivo}";

        var subtituloGM = $"{_mesesAbr[ultimoPer.Mes - 1]} {añoActivo} | {(esCerrado ? "Total" : añoActivo.ToString())}";

        return ApiResponse<KpisDto>.Ok(new KpisDto
        {
            IngresoTotalReal = new KpiItemDto
            {
                Valor      = Math.Round(ingresoReal, 2),
                Unidad     = monedaLabel,
                Semaforo   = ingresoReal >= ingresoPlan ? "Verde" : "Rojo",
                Tendencia  = ingresoReal >= ingresoPlan ? "Arriba" : "Abajo",
                BadgeTexto = ingresoReal >= ingresoPlan ? "Sobre plan" : "Bajo plan",
                Subtitulo  = subtituloRango,
            },
            MargenGM = new KpiItemDto
            {
                Valor      = Math.Round(gmPct, 2),
                Unidad     = "%",
                Semaforo   = gmPct >= 40 ? "Verde" : gmPct >= 35 ? "Amarillo" : "Rojo",
                Tendencia  = gmDelta >= 0 ? "Arriba" : "Abajo",
                BadgeTexto = $"{(gmDelta >= 0 ? "▲" : "▼")} {Math.Abs(gmDelta)} pp vs plan",
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
                Valor      = Math.Round(cumplimiento, 2),
                Unidad     = "%",
                Semaforo   = cumplimiento >= 100 ? "Verde" : cumplimiento >= 90 ? "Amarillo" : "Rojo",
                Tendencia  = cumplimiento >= 100 ? "Arriba" : "Abajo",
                BadgeTexto = $"{(cumplDelta >= 0 ? "▲ +" : "▼ ")}{cumplDelta}%",
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
        var ultimoId = await _db.ConsolidacionLogs
            .Where(l => l.Estado == EstadoConsolidacion.Exitoso)
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

        foreach (var periodo in porPeriodo)
        {
            var totalPeriodo = periodo.Sum(d => d.IngresoReal * d.Factor);
            var prevAño = periodo.Key.Mes == 1 ? periodo.Key.Año - 1 : periodo.Key.Año;
            var prevMes = periodo.Key.Mes == 1 ? 12 : periodo.Key.Mes - 1;
            ingPorPeriodoPrev.TryGetValue((prevAño, prevMes), out var ingPrev);

            var segmentos = periodo
                .GroupBy(d => agruparPor == "area" ? d.Area : d.Industria)
                .Select(sg =>
                {
                    var ing = sg.Sum(d => d.IngresoReal * d.Factor);
                    return new BarrasApiladasItemDto
                    {
                        Periodo                  = Label(periodo.Key.Año, periodo.Key.Mes),
                        Segmento                 = sg.Key,
                        Ingreso                  = Math.Round(ing, 2),
                        PorcentajeContribucion   = totalPeriodo != 0 ? Math.Round(ing / totalPeriodo * 100, 2) : 0m,
                        VariacionPeriodoAnterior = ingPrev != 0 ? Math.Round((ing - ingPrev) / ingPrev * 100, 2) : 0m,
                    };
                });

            items.AddRange(segmentos);
            ingPorPeriodoPrev[(periodo.Key.Año, periodo.Key.Mes)] = totalPeriodo;
        }

        return ApiResponse<BarrasApiladasResponseDto>.Ok(new BarrasApiladasResponseDto
        {
            AgrupadoPor = agruparPor,
            Items       = items,
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // Gráfica 2 — Plan vs Real (últimos 3 meses del año activo)
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<PlanVsRealResponseDto>> GraficaPlanVsRealAsync(ProyectoFiltros filtro)
    {
        // Ignorar filtro de mes; usar año activo (del filtro o máximo en datos)
        var filtroSinMes = filtro with { Mes = null };
        var (datos, _) = await CargarDatosAsync(filtroSinMes);
        if (datos.Count == 0)
            return ApiResponse<PlanVsRealResponseDto>.Ok(new PlanVsRealResponseDto());

        // Año activo = año del filtro (primer valor) o máximo del conjunto
        var añoActivo = filtro.Año?.FirstOrDefault() > 0
            ? filtro.Año.First()
            : datos.Max(d => d.Año);

        // Últimos 3 meses del año activo con datos
        var mesesDisponibles = datos
            .Where(d => d.Año == añoActivo)
            .Select(d => d.Mes)
            .Distinct()
            .OrderByDescending(m => m)
            .Take(3)
            .OrderBy(m => m)
            .ToList();

        var datosFiltrados = datos.Where(d => d.Año == añoActivo && mesesDisponibles.Contains(d.Mes));

        var periodos = datosFiltrados
            .GroupBy(d => new { d.Año, d.Mes })
            .OrderBy(g => g.Key.Mes)
            .Select(g => new PlanVsRealPeriodoDto
            {
                Periodo         = Label(g.Key.Año, g.Key.Mes),
                IngresoPlaneado = Math.Round(g.Sum(d => d.IngresoPlaneado * d.Factor), 2),
                IngresoReal     = Math.Round(g.Sum(d => d.IngresoReal     * d.Factor), 2),
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
                Estado       = variacion >= 0 ? "Verde" : variacion >= -10 ? "Amarillo" : "Rojo",
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

        var puntos = datos
            .GroupBy(d => new { d.Año, d.Mes })
            .OrderBy(g => g.Key.Año).ThenBy(g => g.Key.Mes)
            .Select(g =>
            {
                var real     = g.Sum(d => d.IngresoReal     * d.Factor);
                var plan     = g.Sum(d => d.IngresoPlaneado * d.Factor);
                var variacion   = plan != 0 ? (real - plan) / plan * 100m : 0m;
                var cumpl       = plan != 0 ? real / plan * 100m : 0m;
                return new TendenciaPuntoDto
                {
                    Periodo         = Label(g.Key.Año, g.Key.Mes),
                    IngresoReal     = Math.Round(real,     2),
                    IngresoPlaneado = Math.Round(plan,     2),
                    Variacion       = Math.Round(variacion, 2),
                    PctCumplimiento = Math.Round(cumpl,    2),
                };
            })
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
            .Select(g => new { Cliente = g.Key, Horas = g.Sum(d => d.Horas) })
            .OrderByDescending(x => x.Horas)
            .Take(10)
            .Select(x => new ClienteHorasDto
            {
                Cliente          = x.Cliente,
                Horas            = Math.Round(x.Horas, 2),
                PctParticipacion = totalHoras != 0 ? Math.Round(x.Horas / totalHoras * 100, 2) : 0m,
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
                var ing   = g.Sum(d => d.IngresoReal * d.Factor);
                var costo = g.Sum(d => d.CostoReal   * d.Factor);
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
    // Gráfica 7 — Heatmap GM% por cliente × mes (máx. 5 clientes)
    // ════════════════════════════════════════════════════════════════════════
    public async Task<ApiResponse<HeatmapGmResponseDto>> GraficaHeatmapGmAsync(ProyectoFiltros filtro)
    {
        var (datos, _) = await CargarDatosAsync(filtro);

        // Top 5 clientes por ingreso real
        var top5 = datos
            .GroupBy(d => d.Cliente)
            .OrderByDescending(g => g.Sum(d => d.IngresoReal * d.Factor))
            .Take(5)
            .Select(g => g.Key)
            .ToHashSet();

        var celdas = datos
            .Where(d => top5.Contains(d.Cliente))
            .GroupBy(d => new { d.Cliente, d.Año, d.Mes })
            .Select(g =>
            {
                var ing   = g.Sum(d => d.IngresoReal * d.Factor);
                var costo = g.Sum(d => d.CostoReal   * d.Factor);
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
            .OrderBy(c => c.Cliente).ThenBy(c => c.Periodo)
            .ToList();

        return ApiResponse<HeatmapGmResponseDto>.Ok(new HeatmapGmResponseDto { Celdas = celdas });
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
            Sociedad         = d.Sociedad,
            Ingreso          = Math.Round((d.IngresoReal + d.IngresoPlaneado) * d.Factor, 2),
            Costo            = Math.Round((d.CostoReal   + d.CostoPlaneado)   * d.Factor, 2),
            GM               = Math.Round((d.IngresoReal + d.IngresoPlaneado - d.CostoReal - d.CostoPlaneado) * d.Factor, 2),
            GMPct            = (d.IngresoReal + d.IngresoPlaneado) * d.Factor != 0
                                   ? Math.Round(((d.IngresoReal + d.IngresoPlaneado - d.CostoReal - d.CostoPlaneado) * d.Factor)
                                                / ((d.IngresoReal + d.IngresoPlaneado) * d.Factor) * 100m, 2)
                                   : 0m,
            Horas            = d.Horas,
            TarifaEntrega    = d.Horas != 0
                                   ? Math.Round((d.IngresoReal + d.IngresoPlaneado) * d.Factor / d.Horas, 2)
                                   : 0m,
        }).ToList();

        // Nombre de archivo según HUE-11, usando el último período del dataset
        var ultimo = datos.Count > 0
            ? datos.OrderByDescending(d => d.Año).ThenByDescending(d => d.Mes).First()
            : null;
        var año  = ultimo?.Año  ?? DateTime.UtcNow.Year;
        var mes  = ultimo?.Mes  ?? DateTime.UtcNow.Month;
        var nombreArchivo = $"reporte_ejecutivo_{monedaLabel}_{año}_{mes:D2}.xlsx";

        var stream = new MemoryStream();
        await stream.SaveAsAsync(filas);
        return (stream.ToArray(), nombreArchivo);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Implementaciones existentes (ProyectosController / ProyectoFiltroDto)
    // ════════════════════════════════════════════════════════════════════════
    public Task<ApiResponse<PagedResult<ProyectoDto>>> ObtenerProyectosAsync(ProyectoFiltroDto filtro)
        => Task.FromResult(ApiResponse<PagedResult<ProyectoDto>>.Ok(new PagedResult<ProyectoDto>(),
               "Usar GET /api/proyectos con ProyectoFiltros (Task 16)."));

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
