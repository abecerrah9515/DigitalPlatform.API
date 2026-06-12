namespace DigitalPlatform.Domain.Entities;

public class ReporteCarteraFactura
{
    public int Id { get; set; }
    public int CargaArchivoId { get; set; }

    public string CuentaMayor { get; set; } = string.Empty;
    public string Deudor { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string Asignacion { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public DateTime? FechaDocumento { get; set; }
    public DateTime? FechaContabiliz { get; set; }
    public DateTime? FechaPago { get; set; }
    public string Comentario { get; set; } = string.Empty;
    public string FechaCompromiso { get; set; } = string.Empty;
    public DateTime? FechaPagoReal { get; set; }
    public string AcuerdoPago { get; set; } = string.Empty;
    public string Semana { get; set; } = string.Empty;
    public decimal ImporteMonedaLocal { get; set; }
    public decimal ImporteMonedaDoc { get; set; }
    public decimal ValorRecibir { get; set; }
    public string MonedaDocumento { get; set; } = string.Empty;
    public int DemoraDPP1 { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal VencidoEnTiempo { get; set; }
    public decimal Vencido0_15 { get; set; }
    public decimal Vencido16_30 { get; set; }
    public decimal Vencido31_60 { get; set; }
    public decimal Vencido61_90 { get; set; }
    public decimal Vencido91_120 { get; set; }
    public decimal Vencido121_365 { get; set; }

    public CargaArchivo CargaArchivo { get; set; } = null!;
}
