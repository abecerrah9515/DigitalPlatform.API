namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroTipoCambioDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal Tasa { get; set; }
}
