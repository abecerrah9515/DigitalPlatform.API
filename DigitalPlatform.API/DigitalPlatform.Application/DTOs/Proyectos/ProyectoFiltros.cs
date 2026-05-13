namespace DigitalPlatform.Application.DTOs.Proyectos;

/// <summary>
/// Filtros de Task 16 — todos opcionales, soportan múltiples valores.
/// Query string: ?año=2024&año=2025&mes=1&mes=2&cliente=Bancolombia
/// </summary>
public record ProyectoFiltros
{
    public string   Moneda      { get; set; } = "COP"; // "COP" | "USD"
    public int[]?   Año         { get; set; }
    public int[]?   Mes         { get; set; }
    public string[]? Cliente    { get; set; }
    public string[]? CodProyecto { get; set; }
    public string[]? Vertical   { get; set; } // campo Vertical en Proyectos
    public string[]? Area       { get; set; }
    public string[]? Pais       { get; set; } // campo Pais en Proyectos (UI: "Sociedad")
}
