using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;

namespace DigitalPlatform.Application.Interfaces;

public interface ICargaArchivoService
{
    Task<ApiResponse<List<CargaArchivoDto>>> ObtenerHistorialAsync();
    Task<ApiResponse<string>> SubirArchivoAsync(string tipo, Stream fileStream, string fileName);
}
