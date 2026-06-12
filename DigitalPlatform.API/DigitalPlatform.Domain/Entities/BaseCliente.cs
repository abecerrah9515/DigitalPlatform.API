namespace DigitalPlatform.Domain.Entities;

public class BaseCliente
{
    public int Id { get; set; }
    public int CargaArchivoId { get; set; }

    public string GrupoCuenta { get; set; } = string.Empty;
    public string NumeroCliente { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string CampoClasificacion { get; set; } = string.Empty;
    public string ContactoContabilidad { get; set; } = string.Empty;
    public string ContactoTesoreria { get; set; } = string.Empty;
    public string ContactoFinanzas { get; set; } = string.Empty;
    public string ContactoOperacion { get; set; } = string.Empty;
    public string ContactoComercial { get; set; } = string.Empty;
    public string ContactoCompras { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public string ModificadoPor { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string IdPais { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string IdTipoNif { get; set; } = string.Empty;
    public string TipoNif { get; set; } = string.Empty;
    public string IdClaseImpuesto { get; set; } = string.Empty;
    public string ClaseImpuesto { get; set; } = string.Empty;
    public string PersonaFisica { get; set; } = string.Empty;
    public string Poblacion { get; set; } = string.Empty;
    public string IdGrupoClientes { get; set; } = string.Empty;
    public string GpoClientes { get; set; } = string.Empty;
    public string IdAddenda { get; set; } = string.Empty;
    public string Addenda { get; set; } = string.Empty;

    public CargaArchivo CargaArchivo { get; set; } = null!;
}
