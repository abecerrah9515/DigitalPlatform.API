namespace DigitalPlatform.Application.DTOs.Cartera;

public class NotaClienteDto
{
    public int Id { get; set; }
    public string Autor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Texto { get; set; } = string.Empty;
}
