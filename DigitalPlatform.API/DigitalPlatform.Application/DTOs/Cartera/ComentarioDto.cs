namespace DigitalPlatform.Application.DTOs.Cartera;

public class ComentarioDto
{
    public int Id { get; set; }
    public string Autor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime? NuevaFechaCompromiso { get; set; }
}
