using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalPlatform.Domain.Entities;

public class Proyecto
{
    public int Id { get; set; }
    public int Año { get; set; }
    public int Mes { get; set; }
    public string Industria { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string CodProyecto { get; set; } = string.Empty;
    public string CeBe { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Sociedad { get; set; } = string.Empty;
    public string Vertical { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;

    // FK a la corrida de consolidación que generó este registro
    public int ConsolidacionId { get; set; }
    public ConsolidacionLog Consolidacion { get; set; } = null!;

    // Valores crudos de las fuentes (necesarios para KPIs Plan vs Real)
    public decimal IngresoReal { get; set; }      // ingreso_eu
    public decimal IngresoPlaneado { get; set; }  // ingreso_previsto_eur
    public decimal CostoReal { get; set; }        // coste_eu
    public decimal CostoPlaneado { get; set; }    // coste_previsto_eur

    public decimal Horas { get; set; }

    [NotMapped] public decimal Ingreso        => IngresoReal + IngresoPlaneado;
    [NotMapped] public decimal Costo          => CostoReal + CostoPlaneado;
    [NotMapped] public decimal GM             => Ingreso - Costo;
    [NotMapped] public decimal GMPorcentaje   => Ingreso != 0 ? GM / Ingreso : 0;
    [NotMapped] public decimal TarifaEntrega  => Horas != 0 ? Ingreso / Horas : 0;
}
