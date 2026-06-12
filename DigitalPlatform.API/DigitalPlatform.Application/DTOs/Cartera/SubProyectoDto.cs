namespace DigitalPlatform.Application.DTOs.Cartera;

public class SubProyectoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
