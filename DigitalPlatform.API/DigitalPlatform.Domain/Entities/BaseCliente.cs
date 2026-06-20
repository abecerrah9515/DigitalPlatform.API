namespace DigitalPlatform.Domain.Entities;

public class BaseCliente
{
    public int Id { get; set; }
    public int CargaArchivoId { get; set; }

    public string GrupoCuenta { get; set; } = string.Empty;
    public string NumeroCuenta { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string CampoClas { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string NIT { get; set; } = string.Empty;
    public string NombreContacto { get; set; } = string.Empty;
    public string ContactoContabilidad { get; set; } = string.Empty;
    public string ContactoTesoreria { get; set; } = string.Empty;
    public string ContactoFinanzas { get; set; } = string.Empty;
    public string ContactoOperacion { get; set; } = string.Empty;
    public string ContactoComercial { get; set; } = string.Empty;
    public string ContactoCompras { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoContabilidad { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;

    public CargaArchivo CargaArchivo { get; set; } = null!;
    public List<ContactoCliente> Contactos { get; set; } = [];
}
