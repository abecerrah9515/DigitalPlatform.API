namespace DigitalPlatform.Domain.Entities;

public class ContactoCliente
{
    public int Id { get; set; }
    public int BaseClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;

    public BaseCliente BaseCliente { get; set; } = null!;
}
