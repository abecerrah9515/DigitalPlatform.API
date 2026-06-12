namespace DigitalPlatform.Application.DTOs.Cartera;

public class ClienteDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string EmailContabilidad { get; set; } = string.Empty;
    public string CondicionesPago { get; set; } = string.Empty;
    public string ContactoContabilidad { get; set; } = string.Empty;
    public string ContactoTesoreria { get; set; } = string.Empty;
    public string ContactoFinanzas { get; set; } = string.Empty;
    public string ContactoOperacion { get; set; } = string.Empty;
    public string ContactoComercial { get; set; } = string.Empty;
    public string ContactoCompras { get; set; } = string.Empty;
    public List<NotaClienteDto> Notas { get; set; } = [];
    public List<ContactoClienteDto> Contactos { get; set; } = [];
}
