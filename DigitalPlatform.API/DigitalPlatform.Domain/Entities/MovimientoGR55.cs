namespace DigitalPlatform.Domain.Entities;

/// <summary>
/// Movimiento de GR55 agregado por cuenta contable × período × proyecto.
/// Es la fuente de valores de las hojas numéricas del árbol P&L (HUE-09).
/// Se guardan Cliente y Vertical desnormalizados para filtrar el P&L sin joins.
/// El valor es el mismo que usa el dashboard: ValorMonedaLocalCeBe ya con el signo
/// invertido por el parser (ingresos +, costos −) y en USD-equivalente; el P&L
/// lo multiplica por el Factor (tasaCop en COP, 1 en USD) al calcular.
/// </summary>
public class MovimientoGR55
{
    public int Id { get; set; }
    public string NumeroCuenta { get; set; } = string.Empty;
    public int    Año          { get; set; }
    public int    Mes          { get; set; }
    public string CodProyecto  { get; set; } = string.Empty;
    public string Cliente      { get; set; } = string.Empty;
    public string Vertical     { get; set; } = string.Empty;
    public decimal Valor       { get; set; } // USD-equivalente (real, signo ya invertido como en el dashboard)

    public int ConsolidacionId { get; set; }
    public ConsolidacionLog Consolidacion { get; set; } = null!;
}
