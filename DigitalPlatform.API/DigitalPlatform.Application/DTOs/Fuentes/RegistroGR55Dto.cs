namespace DigitalPlatform.Application.DTOs.Fuentes;

public class RegistroGR55Dto
{
    public string SocReceptora { get; set; } = string.Empty;
    public int PeriodoContable { get; set; }
    public int Ejercicio { get; set; }
    public string NumeroCuenta { get; set; } = string.Empty;
    public string Denominacion { get; set; } = string.Empty;
    public string ElementoPEP { get; set; } = string.Empty;
    public string CentroBeneficio { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public decimal ValorMonedaLocalCeBe { get; set; } // Columna P - signo invertido
    public string ClaveMonedaLocalCeBe { get; set; } = string.Empty;
}
