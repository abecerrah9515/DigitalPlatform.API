namespace DigitalPlatform.Application.DTOs.Proyectos;

// ── GET /api/graficas/barras-apiladas ────────────────────────────────────────
public class BarrasApiladasResponseDto
{
    public string         AgrupadoPor { get; set; } = string.Empty; // "industria" | "area"
    public List<BarrasApiladasItemDto> Items { get; set; } = [];
}

public class BarrasApiladasItemDto
{
    public string  Periodo                  { get; set; } = string.Empty; // "2025-03"
    public string  Segmento                 { get; set; } = string.Empty;
    public decimal Ingreso                  { get; set; }
    public decimal PorcentajeContribucion   { get; set; }
    public decimal VariacionPeriodoAnterior { get; set; }
}

// ── GET /api/graficas/plan-vs-real ───────────────────────────────────────────
public class PlanVsRealResponseDto
{
    public List<PlanVsRealPeriodoDto> Periodos     { get; set; } = [];
    public List<PlanVsRealTablaRowDto> TablaResumen { get; set; } = [];
}

public class PlanVsRealPeriodoDto
{
    public string  Periodo         { get; set; } = string.Empty; // "2025-03"
    public decimal IngresoPlaneado { get; set; }
    public decimal IngresoReal     { get; set; }
}

public class PlanVsRealTablaRowDto
{
    public string  Mes          { get; set; } = string.Empty;
    public decimal Plan         { get; set; }
    public decimal Real         { get; set; }
    public decimal VariacionPct { get; set; }
    public string  Estado       { get; set; } = string.Empty; // Verde | Amarillo | Rojo
}

// ── GET /api/graficas/tendencia ──────────────────────────────────────────────
public class TendenciaResponseDto
{
    public List<TendenciaPuntoDto> Puntos { get; set; } = [];
}

public class TendenciaPuntoDto
{
    public string  Periodo          { get; set; } = string.Empty; // "2025-03"
    public decimal IngresoReal      { get; set; }
    public decimal IngresoPlaneado  { get; set; }
    public decimal Variacion        { get; set; }
    public decimal PctCumplimiento  { get; set; }
}

// ── GET /api/graficas/top-clientes-horas ────────────────────────────────────
public class TopClientesHorasResponseDto
{
    public List<ClienteHorasDto> Clientes { get; set; } = [];
}

public class ClienteHorasDto
{
    public string  Cliente          { get; set; } = string.Empty;
    public decimal Horas            { get; set; }
    public decimal PctParticipacion { get; set; }
}

// ── GET /api/graficas/treemap-area ───────────────────────────────────────────
public class TreemapAreaResponseDto
{
    public List<AreaHorasDto> Areas { get; set; } = [];
}

public class AreaHorasDto
{
    public string  Area              { get; set; } = string.Empty;
    public decimal Horas             { get; set; }
    public int     CantidadProyectos { get; set; }
    public decimal PctParticipacion  { get; set; }
}

// ── GET /api/graficas/scatter-burbuja ────────────────────────────────────────
public class ScatterBurbujaResponseDto
{
    public List<BurbujaClienteDto> Clientes   { get; set; } = [];
    public decimal                 TarifaPromedio { get; set; } // línea vertical de referencia
}

public class BurbujaClienteDto
{
    public string  Cliente       { get; set; } = string.Empty;
    public string  Area          { get; set; } = string.Empty;
    public decimal TarifaEntrega { get; set; } // eje X
    public decimal GmPct         { get; set; } // eje Y
    public decimal Ingreso       { get; set; } // tamaño de la burbuja
}

// ── GET /api/graficas/heatmap-gm ────────────────────────────────────────────
public class HeatmapGmResponseDto
{
    public List<HeatmapCeldaDto> Celdas { get; set; } = [];
}

public class HeatmapCeldaDto
{
    public string  Cliente { get; set; } = string.Empty;
    public string  Periodo { get; set; } = string.Empty; // "2025-03"
    public decimal GmPct   { get; set; }
    public decimal Ingreso { get; set; }
    public decimal Costo   { get; set; }
}
