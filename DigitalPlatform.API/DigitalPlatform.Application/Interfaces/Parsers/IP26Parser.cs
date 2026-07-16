using DigitalPlatform.Application.DTOs.Fuentes;

namespace DigitalPlatform.Application.Interfaces.Parsers;

public interface IP26Parser
{
    Task<List<RegistroP26Dto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null);
}
