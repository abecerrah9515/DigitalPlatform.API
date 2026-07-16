namespace DigitalPlatform.Application.DTOs.Pyl;

/// <summary>Un nodo (fila) del árbol P&L con sus 12 valores mensuales y el acumulado.</summary>
public class PnlNodoDto
{
    public string LineItemId     { get; set; } = string.Empty;
    // Etiqueta de la columna "P&L Real" (HUE-08): agrupador → Account Name;
    // último nivel → "LineItemId Account Name".
    public string Etiqueta       { get; set; } = string.Empty;
    public string AccountName    { get; set; } = string.Empty;
    public string ParentId       { get; set; } = string.Empty;
    public int    Nivel          { get; set; }
    public string TipoFinanciero { get; set; } = string.Empty;
    public bool   TieneFormula   { get; set; }
    public bool   TieneHijos     { get; set; }
    public bool   EsHoja         { get; set; } // LineItemId numérico (cuenta GR55)
    public bool   SinMovimientos { get; set; } // hoja sin registros en GR55 → mostrar vacío
    public decimal[] Valores     { get; set; } = new decimal[12]; // Ene..Dic
    public decimal Acum          { get; set; }
    public decimal Descuadre     { get; set; } // valor − suma(hijos); ≠0 → ícono de advertencia
    public int    Orden          { get; set; }
}

/// <summary>Respuesta de la tabla P&L: árbol plano ordenado, con año y estado.</summary>
public class PnlResponseDto
{
    public int?  Año         { get; set; }
    public bool  RequiereAño { get; set; } // true → mostrar "Seleccione un año…"
    public string Moneda     { get; set; } = "COP";
    public List<PnlNodoDto> Nodos { get; set; } = [];
}

/// <summary>Valores disponibles para los filtros del módulo P&L (HUE-07).</summary>
public class PnlFiltrosDto
{
    public List<string> Clientes  { get; set; } = [];
    public List<string> Proyectos { get; set; } = [];
    public List<string> Verticales { get; set; } = [];
    public List<int>    Años      { get; set; } = [];
}
