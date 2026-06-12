namespace DigitalPlatform.Application.DTOs.Cartera;

public class ProyeccionPagoDto
{
    public string RazonSocial { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int FacturasPendientes { get; set; }
}
