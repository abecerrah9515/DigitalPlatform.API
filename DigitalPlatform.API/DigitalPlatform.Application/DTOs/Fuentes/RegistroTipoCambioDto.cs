namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroTipoCambioDto
{
    public DateOnly Fecha   { get; set; }
    public int      Año     { get; set; }
    public int      Mes     { get; set; }
    public decimal  TasaCop { get; set; }
    public decimal  TasaUsd { get; set; }
}
