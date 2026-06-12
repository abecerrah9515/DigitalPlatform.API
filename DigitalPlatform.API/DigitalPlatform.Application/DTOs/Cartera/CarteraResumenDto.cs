namespace DigitalPlatform.Application.DTOs.Cartera;

public class CarteraResumenDto
{
    public decimal FacturasPorCobrar { get; set; }
    public decimal FacturasVencidas { get; set; }
    public decimal FacturasConfirmadas { get; set; }
    public int FacturasConDiferencia { get; set; }
    public string Moneda { get; set; } = string.Empty;
}
