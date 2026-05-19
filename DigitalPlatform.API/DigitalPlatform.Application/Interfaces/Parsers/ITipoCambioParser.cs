using DigitalPlatform.Application.DTOs.Fuentes;

namespace DigitalPlatform.Application.Interfaces.Parsers;

public interface ITipoCambioParser
{
    Task<List<RegistroTipoCambioDto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null);
}
