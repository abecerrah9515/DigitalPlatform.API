namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroTipoCambioDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal Tarifa { get; set; } // Tasa USD → COP
}
