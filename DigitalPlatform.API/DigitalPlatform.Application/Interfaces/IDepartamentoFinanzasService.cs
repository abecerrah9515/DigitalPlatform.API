using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;

namespace DigitalPlatform.Application.Interfaces;

public interface IDepartamentoFinanzasService
{
    Task<ApiResponse<List<DepartamentoFinanzasDto>>> ObtenerTodosAsync();
}
