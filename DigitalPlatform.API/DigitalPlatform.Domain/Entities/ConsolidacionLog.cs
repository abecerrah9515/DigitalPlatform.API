using DigitalPlatform.Domain.Enums;

namespace DigitalPlatform.Domain.Entities;

public class ConsolidacionLog
{
    public int Id { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public EstadoConsolidacion Estado { get; set; }
    public int TotalRegistros { get; set; }
    public int RegistrosExitosos { get; set; }
    public int RegistrosFallidos { get; set; }
    public string? Errores { get; set; }
    /// <summary>JSON serializado de List&lt;FuenteEstadoDto&gt; — se guarda al completar la consolidación.</summary>
    public string? FuentesJson { get; set; }
    public string IniciadoPor { get; set; } = string.Empty;

    public ICollection<Proyecto> Proyectos { get; set; } = [];
}
