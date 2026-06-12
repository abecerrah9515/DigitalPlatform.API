namespace DigitalPlatform.Application.DTOs.Cartera;

public class SeguimientoUrgenteDto
{
    public string Cliente { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public List<CarteraFacturaDto> Facturas { get; set; } = [];
}
