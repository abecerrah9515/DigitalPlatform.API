namespace DigitalPlatform.Application.DTOs.Proyectos;

public class ProyectoDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public string Industria { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string CodProyecto { get; set; } = string.Empty;
    public string CeBe { get; set; } = string.Empty;
    public string Responsable { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Sociedad { get; set; } = string.Empty;
    public decimal Ingreso { get; set; }
    public decimal Costo { get; set; }
    public decimal GM { get; set; }
    public decimal GMPorcentaje { get; set; }
    public decimal Horas { get; set; }
    public decimal TarifaEntrega { get; set; }
}
