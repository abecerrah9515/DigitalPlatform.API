using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Pyl;

namespace DigitalPlatform.Application.Interfaces;

public interface IPnlService
{
    Task<ApiResponse<PnlResponseDto>> ObtenerPnlAsync(PnlFiltros filtro);
    Task<ApiResponse<PnlFiltrosDto>>  ObtenerFiltrosAsync(PnlFiltros filtro);
}
