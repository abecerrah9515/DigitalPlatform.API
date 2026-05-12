namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroPlaneacionDto
{
    public string Cliente { get; set; } = string.Empty;
    public string Proyecto { get; set; } = string.Empty;
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal IngresoPrevistoEur { get; set; }
    public decimal CostePrevistoEur { get; set; }
    public string Cebe { get; set; } = string.Empty;
    public string Industria { get; set; } = string.Empty;
    public string Brm { get; set; } = string.Empty;
    public string ResponsableWbs { get; set; } = string.Empty;
}
