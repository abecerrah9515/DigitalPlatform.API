using DigitalPlatform.Application.DTOs.Fuentes;

namespace DigitalPlatform.Application.Interfaces.Parsers;

public interface IHorasParser
{
    // Solo filas donde ESTADO = 'Accepted'
    Task<List<RegistroHorasDto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null);
}
