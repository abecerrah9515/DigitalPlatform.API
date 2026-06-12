namespace DigitalPlatform.Application.DTOs.Cartera;

public class NotificacionEnviadaDto
{
    public int Id { get; set; }
    public string Factura { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
