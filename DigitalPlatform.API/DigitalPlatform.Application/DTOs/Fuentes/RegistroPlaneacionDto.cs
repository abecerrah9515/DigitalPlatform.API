namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroPlaneacionDto
{
    public string ElementoPEP { get; set; } = string.Empty;
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal IngresoPrevistoEur { get; set; }
    public decimal CostePrevistoEur { get; set; }
}
