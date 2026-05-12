namespace DigitalPlatform.Application.DTOs.Proyectos;

public class GraficoBarrasApiladasDto
{
    public string VistaActual { get; set; } = string.Empty; // Industria, Area
    public List<BarraApiladaPeriodoDto> Periodos { get; set; } = [];
}

public class BarraApiladaPeriodoDto
{
    public int Año { get; set; }
    public int Mes { get; set; }
    public List<SegmentoDto> Segmentos { get; set; } = [];
}

public class SegmentoDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Ingreso { get; set; }
    public decimal PorcentajeContribucion { get; set; }
}
