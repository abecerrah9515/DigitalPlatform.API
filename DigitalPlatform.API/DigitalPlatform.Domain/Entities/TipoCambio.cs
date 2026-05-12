namespace DigitalPlatform.Domain.Entities;

public class TipoCambio
{
    public int Id { get; set; }
    public int Año { get; set; }
    public int Mes { get; set; }
    public string Moneda { get; set; } = string.Empty; // COP, USD, EUR
    public decimal Tasa { get; set; }
}
