namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroGR55Dto
{
    public string ElementoPEP { get; set; } = string.Empty;
    public string Sociedad { get; set; } = string.Empty;
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal IngresoEu { get; set; }
    public decimal CosteEu { get; set; }
}
