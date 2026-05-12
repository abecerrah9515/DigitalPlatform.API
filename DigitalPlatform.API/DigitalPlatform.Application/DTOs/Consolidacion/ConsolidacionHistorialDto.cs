namespace DigitalPlatform.Application.DTOs.Consolidacion;

public class ConsolidacionHistorialDto
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int TotalRegistros { get; set; }
    public int RegistrosExitosos { get; set; }
    public int RegistrosFallidos { get; set; }
    public string IniciadoPor { get; set; } = string.Empty;
}
