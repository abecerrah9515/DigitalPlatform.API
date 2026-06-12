namespace DigitalPlatform.Application.DTOs.Cartera;

public class CarteraFacturaDto
{
    public int Id { get; set; }
    public string Factura { get; set; } = string.Empty;
    public string Consecutivo { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal Retencion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int DiasMora { get; set; }
}
