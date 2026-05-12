namespace DigitalPlatform.Application.DTOs.Consolidacion;

public class ConsolidacionIniciadaDto
{
    public int ConsolidacionId { get; set; }
    public DateTime FechaInicio { get; set; }
    public string Estado { get; set; } = string.Empty;
}
