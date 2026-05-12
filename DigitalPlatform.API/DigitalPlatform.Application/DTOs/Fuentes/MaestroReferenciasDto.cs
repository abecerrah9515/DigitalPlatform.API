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
    public string LineItemId    { get; set; } = string.Empty;
    public string Account       { get; set; } = string.Empty;
    public string Clasificacion { get; set; } = string.Empty; // "Ingreso" | "Costo"
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
