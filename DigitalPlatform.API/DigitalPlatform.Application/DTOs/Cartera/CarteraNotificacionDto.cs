namespace DigitalPlatform.Application.DTOs.Cartera;

public class CarteraNotificacionDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Factura { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public int DiasMora { get; set; }
    public decimal Monto { get; set; }
}
