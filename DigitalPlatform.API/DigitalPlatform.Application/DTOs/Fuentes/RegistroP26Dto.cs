namespace DigitalPlatform.Application.DTOs.Fuentes;

/// <summary>
/// Registro del archivo de plan Arch.P26 (referencia de comparación del dashboard).
/// El plan está a nivel de Source.Name (vertical/industria), no por proyecto.
/// </summary>
public class RegistroP26Dto
{
    public string SourceName { get; set; } = string.Empty; // ej. "01BFS.xlsx"
    public string Task       { get; set; } = string.Empty; // ej. "1.3.1 Service Revenue"
    public int    Año        { get; set; }                 // Year
    public int    Mes        { get; set; }                 // MonthNum (1-12)
    public decimal Amount    { get; set; }
}
