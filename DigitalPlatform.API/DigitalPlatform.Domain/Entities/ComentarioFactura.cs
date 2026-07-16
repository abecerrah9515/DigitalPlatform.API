namespace DigitalPlatform.Domain.Entities;

public class ComentarioFactura
{
    public int Id { get; set; }
    public int CargaArchivoId { get; set; }
    public int FacturaId { get; set; }
    public string Autor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime? NuevaFechaCompromiso { get; set; }
    public string? FacturaNumero { get; set; }
    public string? ClienteNombre { get; set; }

    public CargaArchivo CargaArchivo { get; set; } = null!;
}
