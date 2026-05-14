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
    public decimal Valor      { get; set; }
    public string  Unidad     { get; set; } = string.Empty; // $, %, h
    public string  Semaforo   { get; set; } = string.Empty; // Verde, Amarillo, Rojo, Gris
    public string  Tendencia  { get; set; } = string.Empty; // Arriba, Abajo, Neutro
    public string  BadgeTexto { get; set; } = string.Empty; // "Sobre plan", "▲ +3.2 pp", "PROJ-001"
    public string  Subtitulo  { get; set; } = string.Empty; // "Ene–May 2026", "May 2026 | Total"
}
