namespace DigitalPlatform.Application.DTOs.Proyectos;

public class KpisDto
{
    public KpiItemDto IngresoTotalReal { get; set; } = new();
    public KpiItemDto MargenGM { get; set; } = new();
    public KpiItemDto HorasEntregadas { get; set; } = new();
    public KpiItemDto TarifaEntregaPromedio { get; set; } = new();
    public KpiItemDto CumplimientoIngresosPlan { get; set; } = new();
}

public class KpiItemDto
{
    public decimal Valor { get; set; }
    public string Unidad { get; set; } = string.Empty;   // $, %, h
    public string Semaforo { get; set; } = string.Empty; // Verde, Amarillo, Rojo, Gris
    public string Tendencia { get; set; } = string.Empty; // Arriba, Abajo
}
