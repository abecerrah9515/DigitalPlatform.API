namespace DigitalPlatform.Application.DTOs.Cartera;

public class FechaReprogramadaDto
{
    public string Factura { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime NuevaFechaCompromiso { get; set; }
    public string Texto { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
