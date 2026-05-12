namespace DigitalPlatform.Application.DTOs.Fuentes;

public class MaestroReferenciasDto
{
    public List<SociedadReferenciaDto> Sociedades { get; set; } = [];
    public List<CeBeReferenciaDto> CeBes { get; set; } = [];
    public List<IndustriaReferenciaDto> Industrias { get; set; } = [];
}

public class SociedadReferenciaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
}

public class CeBeReferenciaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string CeBeGroup { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class IndustriaReferenciaDto
{
    public string CodIndustria { get; set; } = string.Empty;
    public string Vertical { get; set; } = string.Empty;
}
