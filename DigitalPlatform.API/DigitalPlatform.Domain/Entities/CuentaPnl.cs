namespace DigitalPlatform.Domain.Entities;

/// <summary>
/// Nodo del árbol de cuentas del P&L (hoja Accounts_Group del maestro).
/// Un LineItemId numérico es una hoja cuyo valor sale de Arch.GR55; un código
/// agrupador (AGR-...) suma sus hijos directos o evalúa su fórmula (Referencia).
/// </summary>
public class CuentaPnl
{
    public int Id { get; set; }
    public string LineItemId     { get; set; } = string.Empty;
    public string AccountName    { get; set; } = string.Empty;
    public string ParentId       { get; set; } = string.Empty; // vacío = raíz (Nivel 1)
    public int    Nivel          { get; set; }
    public string TipoFinanciero { get; set; } = string.Empty; // Ingreso | Costos | (vacío)
    public string Referencia     { get; set; } = string.Empty; // fórmula opcional sobre otros LineItemId
    public int    Orden          { get; set; }                 // orden original en el maestro

    public int ConsolidacionId { get; set; }
    public ConsolidacionLog Consolidacion { get; set; } = null!;
}
