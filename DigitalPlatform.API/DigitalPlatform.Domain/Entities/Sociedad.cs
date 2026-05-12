namespace DigitalPlatform.Domain.Entities;

public class Sociedad
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
}
