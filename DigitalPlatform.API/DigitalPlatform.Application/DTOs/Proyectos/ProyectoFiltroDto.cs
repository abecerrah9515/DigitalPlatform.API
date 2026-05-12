namespace DigitalPlatform.Application.DTOs.Proyectos;

public class ProyectoFiltroDto
{
    public string? Cliente { get; set; }
    public string? CodProyecto { get; set; }
    public string? Industria { get; set; }
    public string? Area { get; set; }
    public string? Sociedad { get; set; }
    public string Moneda { get; set; } = "USD";
    public int? Año { get; set; }
    public int? Mes { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamañoPagina { get; set; } = 10;
}
