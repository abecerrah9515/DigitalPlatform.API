using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Consolidacion;

namespace DigitalPlatform.Application.Interfaces;

public interface IConsolidacionService
{
    /// <summary>Crea el ConsolidacionLog con estado Procesando y retorna su Id.</summary>
    Task<int> CrearLogAsync(string iniciadoPor);

    /// <summary>Ejecuta los 5 parsers y persiste proyectos. Diseñado para correr en background.</summary>
    Task IniciarConsolidacionAsync(int consolidacionId);

    Task<ApiResponse<ConsolidacionEstadoDto>> ObtenerEstadoAsync(int consolidacionId);
    Task<ApiResponse<PagedResult<ConsolidacionHistorialDto>>> ObtenerHistorialAsync(int pagina, int tamañoPagina);
}
