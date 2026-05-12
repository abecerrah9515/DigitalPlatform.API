namespace DigitalPlatform.Application.DTOs.Consolidacion;

public class ConsolidacionEstadoDto
{
    public int ConsolidacionId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int PorcentajeAvance { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosExitosos { get; set; }
    public int RegistrosFallidos { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public List<string> Errores { get; set; } = [];
}
