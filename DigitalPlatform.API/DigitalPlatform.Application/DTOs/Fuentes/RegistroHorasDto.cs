namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroHorasDto
{
    public string ElementoPEP { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string NombreResponsable { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal HorasImputadas { get; set; }
}
