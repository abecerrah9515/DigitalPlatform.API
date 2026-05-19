using DigitalPlatform.Application.DTOs.Fuentes;

namespace DigitalPlatform.Application.Interfaces.Parsers;

public interface IPlaneacionParser
{
    Task<List<RegistroPlaneacionDto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null);
}
