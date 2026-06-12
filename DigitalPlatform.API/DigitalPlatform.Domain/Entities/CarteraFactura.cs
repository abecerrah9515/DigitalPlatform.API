namespace DigitalPlatform.Domain.Entities;

public class CarteraFactura
{
    public int Id { get; set; }
    public string CuentaMayor { get; set; } = string.Empty;
    public string Deudor { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string Asignacion { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public DateTime FechaDocumento { get; set; }
    public DateTime FechaContabilizacion { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? Comentario { get; set; }
    public DateTime? FechaConfirmacionPago { get; set; }
    public DateTime? FechaPagoReal { get; set; }
    public string? AcuerdoPago { get; set; }
    public string? Semana { get; set; }
    public decimal ImporteMonedaLocal { get; set; }
    public decimal ImporteMonedaDoc { get; set; }
    public decimal ValorRecibir { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public int DiasMora { get; set; }
    public string Estado { get; set; } = string.Empty;
}
