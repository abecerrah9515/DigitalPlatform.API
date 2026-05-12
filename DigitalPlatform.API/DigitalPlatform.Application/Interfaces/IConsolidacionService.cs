using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Consolidacion;

namespace DigitalPlatform.Application.Interfaces;

public interface IConsolidacionService
{
    Task<ApiResponse<ConsolidacionIniciadaDto>> IniciarConsolidacionAsync(string iniciadoPor);
    Task<ApiResponse<ConsolidacionEstadoDto>> ObtenerEstadoAsync(int consolidacionId);
    Task<ApiResponse<PagedResult<ConsolidacionHistorialDto>>> ObtenerHistorialAsync(int pagina, int tamañoPagina);
}
