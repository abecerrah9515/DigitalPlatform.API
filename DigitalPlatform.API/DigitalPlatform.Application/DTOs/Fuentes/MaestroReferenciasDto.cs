namespace DigitalPlatform.Application.DTOs.Fuentes;

public class MaestroReferenciasDto
{
    public List<IndustriaReferenciaDto>     Industrias     { get; set; } = [];
    public List<CeBeReferenciaDto>          CeBes          { get; set; } = [];
    public List<SociedadReferenciaDto>      Sociedades     { get; set; } = [];
    public List<PaisReferenciaDto>          Paises         { get; set; } = [];
    public List<AccountsGroupReferenciaDto> AccountsGroups { get; set; } = [];
    public List<VerticalReferenciaDto>      Verticales     { get; set; } = [];
    public List<AreaReferenciaDto>          Areas          { get; set; } = [];
    public List<ResponsableReferenciaDto>   Responsables   { get; set; } = [];
}

public class IndustriaReferenciaDto
{
    public string CodIndustria { get; set; } = string.Empty;
    public string Vertical     { get; set; } = string.Empty;
}

public class CeBeReferenciaDto
{
    public string CeBeGroup { get; set; } = string.Empty;
    public string CeBe      { get; set; } = string.Empty;
    public string Nombre    { get; set; } = string.Empty;
}

public class SociedadReferenciaDto
{
    public string Sociedad   { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Pais       { get; set; } = string.Empty;
}

public class PaisReferenciaDto
{
    public string ISOCode { get; set; } = string.Empty;
    public string Pais    { get; set; } = string.Empty;
}

public class AccountsGroupReferenciaDto
{
    public string LineItemId    { get; set; } = string.Empty; // cuenta numérica u código agrupador (AGR-...)
    public string Account       { get; set; } = string.Empty; // "Account Name"
    public string Clasificacion { get; set; } = string.Empty; // "Tipo Financiero": Ingreso | Costos | null
    public string ParentId      { get; set; } = string.Empty; // Parent ID (LineItemId del padre; vacío = raíz)
    public int    Nivel         { get; set; }                 // profundidad en el árbol (1..5)
    public string Referencia    { get; set; } = string.Empty; // fórmula sobre otros LineItemId (opcional)
}

public class VerticalReferenciaDto
{
    public string Vertical    { get; set; } = string.Empty;
    public string CodIndustria { get; set; } = string.Empty;
}

public class AreaReferenciaDto
{
    public string Area { get; set; } = string.Empty;
    public string CeBe { get; set; } = string.Empty;
}

public class ResponsableReferenciaDto
{
    public string ResponsableWbs  { get; set; } = string.Empty; // responsable_wbs (ID)
    public string ResponsableName { get; set; } = string.Empty; // responsable_name (nombre completo)
}
