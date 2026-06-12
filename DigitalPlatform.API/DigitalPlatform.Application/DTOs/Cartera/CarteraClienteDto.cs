namespace DigitalPlatform.Application.DTOs.Cartera;

public class CarteraClienteDto
{
    public string Cliente { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public int FacturasPendientes { get; set; }
}
