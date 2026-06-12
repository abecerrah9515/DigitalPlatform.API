namespace DigitalPlatform.Application.DTOs.Cartera;

public class CargaArchivoDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
    public int TotalRegistros { get; set; }
}
