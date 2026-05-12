using DigitalPlatform.Application.DTOs.Fuentes;

namespace DigitalPlatform.Application.Interfaces.Parsers;

public interface IGR55Parser
{
    // Solo filas donde Elemento PEP tenga dato
    Task<List<RegistroGR55Dto>> ParsearAsync(Stream archivo);
}
