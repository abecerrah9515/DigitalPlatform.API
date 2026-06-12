namespace DigitalPlatform.Application.DTOs.Cartera;

public class ProgramacionPagoDto
{
    public int Id { get; set; }
    public string Factura { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string FechaCompromiso { get; set; } = string.Empty;
    public int Dias { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string SemanaFormateada { get; set; } = string.Empty;
    public decimal ImporteMonedaLocal { get; set; }
}
