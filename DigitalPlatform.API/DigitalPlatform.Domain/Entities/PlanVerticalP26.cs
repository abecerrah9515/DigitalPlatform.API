namespace DigitalPlatform.Domain.Entities;

/// <summary>
/// Plan de referencia Arch.P26 agregado por Vertical × Año × Mes.
/// Es la baseline de comparación del dashboard (badges, diferenciales,
/// cumplimiento, líneas "planeado"). No reemplaza el valor proyectado de
/// Planeación; es una dimensión aparte a nivel de vertical/portafolio.
/// </summary>
public class PlanVerticalP26
{
    public int Id { get; set; }
    public string Vertical { get; set; } = string.Empty;
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal IngresoPlan { get; set; }
    public decimal CostoPlan { get; set; }

    // FK a la corrida de consolidación que generó este registro
    public int ConsolidacionId { get; set; }
    public ConsolidacionLog Consolidacion { get; set; } = null!;
}
