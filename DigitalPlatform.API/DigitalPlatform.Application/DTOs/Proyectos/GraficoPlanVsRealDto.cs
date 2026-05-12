namespace DigitalPlatform.Application.DTOs.Proyectos;

public class GraficoPlanVsRealDto
{
    public List<PeriodoPlanVsRealDto> Periodos { get; set; } = [];
    public List<TablaPlanVsRealDto> TablaResumen { get; set; } = [];
}

public class PeriodoPlanVsRealDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public decimal IngresoReal { get; set; }
    public decimal IngresoPlaneado { get; set; }
}

public class TablaPlanVsRealDto
{
    public string Mes { get; set; } = string.Empty;
    public decimal Plan { get; set; }
    public decimal Real { get; set; }
    public decimal DeltaPorcentaje { get; set; }
}
