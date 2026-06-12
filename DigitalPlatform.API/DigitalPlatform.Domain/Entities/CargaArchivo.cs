namespace DigitalPlatform.Domain.Entities;

public class CargaArchivo
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
    public int TotalRegistros { get; set; }
    public string? RutaArchivo { get; set; }
}
