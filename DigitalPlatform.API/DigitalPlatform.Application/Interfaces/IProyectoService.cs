using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Proyectos;

namespace DigitalPlatform.Application.Interfaces;

public interface IProyectoService
{
    // ── Endpoints existentes (ProyectosController, ProyectoFiltroDto) ────────
    Task<ApiResponse<PagedResult<ProyectoDto>>>      ObtenerProyectosAsync(ProyectoFiltroDto filtro);
    Task<ApiResponse<KpisDto>>                        ObtenerKpisAsync(ProyectoFiltroDto filtro);
    Task<ApiResponse<GraficoBarrasApiladasDto>>       ObtenerBarrasApiladasAsync(ProyectoFiltroDto filtro);
    Task<ApiResponse<GraficoPlanVsRealDto>>           ObtenerPlanVsRealAsync(ProyectoFiltroDto filtro);
    Task<byte[]>                                      DescargarExcelAsync(ProyectoFiltroDto filtro);

    // ── Task 16 — KPIs (GET /api/kpis) ──────────────────────────────────────
    Task<ApiResponse<KpisDto>> ObtenerKpisAsync(ProyectoFiltros filtro);

    // ── Task 16 — Filtros dependientes (GET /api/graficas/filtros/valores) ───
    Task<ApiResponse<FiltrosValoresDto>> ObtenerFiltrosValoresAsync(ProyectoFiltros filtro);

    // ── Task 22 — Descarga Excel (GET /api/graficas/descargar) ───────────────
    Task<(byte[] Bytes, string NombreArchivo)> DescargarExcelFiltradoAsync(ProyectoFiltros filtro);

    // ── Task 16 — 7 gráficas (GET /api/graficas/{nombre}) ───────────────────
    Task<ApiResponse<BarrasApiladasResponseDto>>       GraficaBarrasApiladasAsync(ProyectoFiltros filtro, string agruparPor);
    Task<ApiResponse<PlanVsRealResponseDto>>           GraficaPlanVsRealAsync(ProyectoFiltros filtro);
    Task<ApiResponse<TendenciaResponseDto>>            GraficaTendenciaAsync(ProyectoFiltros filtro);
    Task<ApiResponse<TopClientesHorasResponseDto>>     GraficaTopClientesHorasAsync(ProyectoFiltros filtro);
    Task<ApiResponse<TreemapAreaResponseDto>>           GraficaTreemapAreaAsync(ProyectoFiltros filtro);
    Task<ApiResponse<ScatterBurbujaResponseDto>>       GraficaScatterBurbujaAsync(ProyectoFiltros filtro);
    Task<ApiResponse<HeatmapGmResponseDto>>            GraficaHeatmapGmAsync(ProyectoFiltros filtro, int pagina = 1);
}
