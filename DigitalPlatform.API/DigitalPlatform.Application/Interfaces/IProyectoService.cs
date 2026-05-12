using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Proyectos;

namespace DigitalPlatform.Application.Interfaces;

public interface IProyectoService
{
    Task<ApiResponse<PagedResult<ProyectoDto>>> ObtenerProyectosAsync(ProyectoFiltroDto filtro);
    Task<ApiResponse<KpisDto>> ObtenerKpisAsync(ProyectoFiltroDto filtro);
    Task<ApiResponse<GraficoBarrasApiladasDto>> ObtenerBarrasApiladasAsync(ProyectoFiltroDto filtro);
    Task<ApiResponse<GraficoPlanVsRealDto>> ObtenerPlanVsRealAsync(ProyectoFiltroDto filtro);
    Task<byte[]> DescargarExcelAsync(ProyectoFiltroDto filtro);
}
