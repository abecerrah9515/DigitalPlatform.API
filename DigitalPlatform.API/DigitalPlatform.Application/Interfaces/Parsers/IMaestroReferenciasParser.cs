using DigitalPlatform.Application.DTOs.Fuentes;

namespace DigitalPlatform.Application.Interfaces.Parsers;

public interface IMaestroReferenciasParser
{
    Task<MaestroReferenciasDto> ParsearAsync(Stream archivo, Action<int>? onProgress = null);
}
