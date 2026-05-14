namespace DigitalPlatform.Application.DTOs.Proyectos;

public class FiltrosValoresDto
{
    public List<string> Clientes   { get; set; } = [];
    public List<string> Proyectos  { get; set; } = []; // CodProyecto
    public List<string> Verticales { get; set; } = []; // VFU
    public List<string> Areas      { get; set; } = [];
    public List<string> Paises     { get; set; } = []; // UI label: Sociedad
    public List<int>    Años       { get; set; } = [];
    public List<int>    Meses      { get; set; } = [];
}
