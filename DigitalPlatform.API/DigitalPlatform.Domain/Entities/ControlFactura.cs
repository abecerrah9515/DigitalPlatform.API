namespace DigitalPlatform.Domain.Entities;

public class ControlFactura
{
    public int Id { get; set; }
    public int CargaArchivoId { get; set; }

    public DateTime? FechaSolicito { get; set; }
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? FechaPago { get; set; }
    public string NumeroCliente { get; set; } = string.Empty;
    public string Pep { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal Iva { get; set; }
    public decimal ReteIva { get; set; }
    public decimal Autorenta { get; set; }
    public decimal Retencion { get; set; }
    public decimal Ica { get; set; }
    public decimal Total { get; set; }
    public string OrdenConsecutivo { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string OrdenPedido { get; set; } = string.Empty;
    public string EntradaMercancia { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string DatosAdicionales { get; set; } = string.Empty;
    public decimal Trm { get; set; }
    public DateTime? DiaTrm { get; set; }
    public decimal ValorUsd { get; set; }
    public DateTime? FechaSolicitud { get; set; }
    public decimal ValorAnulacion { get; set; }
    public string ServicioProducto { get; set; } = string.Empty;
    public string RazonAnulacion { get; set; } = string.Empty;
    public string NotaCredito { get; set; } = string.Empty;
    public string NumeroDocumento2 { get; set; } = string.Empty;
    public string Compensacion { get; set; } = string.Empty;
    public string Reemplazo { get; set; } = string.Empty;
    public decimal ValorCancelar { get; set; }
    public string ActaNumero { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;

    public CargaArchivo CargaArchivo { get; set; } = null!;
}
