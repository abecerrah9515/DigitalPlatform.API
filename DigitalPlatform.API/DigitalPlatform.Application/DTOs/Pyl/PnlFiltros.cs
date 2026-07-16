namespace DigitalPlatform.Application.DTOs.Pyl;

/// <summary>
/// Filtros del módulo P&L (HUE-07). Independientes del dashboard: Cliente, Proyecto,
/// Vertical y Año. "Todas" = sin restricción (arrays nulos/vacíos).
/// </summary>
public record PnlFiltros
{
    public string   Moneda    { get; set; } = "COP"; // COP | USD
    public int?     Año       { get; set; }          // requerido para ver la tabla
    public string[]? Cliente  { get; set; }
    public string[]? Proyecto { get; set; }
    public string[]? Vertical { get; set; }
    public int?     ConsolidacionId { get; set; }    // opcional: ver una corrida anterior
}
