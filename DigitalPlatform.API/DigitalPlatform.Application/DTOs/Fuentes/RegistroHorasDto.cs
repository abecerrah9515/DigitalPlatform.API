namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroHorasDto
{
    public string TrabajadorId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Ceco { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public string Sociedad { get; set; } = string.Empty;
    public string Industria { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty; // proyecto_area (ej. "AMS", "DIGITAL")
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal Horas { get; set; }
    public string Brm { get; set; } = string.Empty;
}
