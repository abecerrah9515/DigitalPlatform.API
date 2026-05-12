using ClosedXML.Excel;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

public class MaestroReferenciasParser : IMaestroReferenciasParser
{
    private readonly ILogger<MaestroReferenciasParser> _logger;

    public MaestroReferenciasParser(ILogger<MaestroReferenciasParser> logger) => _logger = logger;

    public Task<MaestroReferenciasDto> ParsearAsync(Stream archivo)
    {
        var dto = new MaestroReferenciasDto();

        using var wb = new XLWorkbook(archivo);

        dto.Industrias     = ParseIndustrias(wb);
        dto.CeBes          = ParseCeBes(wb);
        dto.Sociedades     = ParseSociedades(wb);
        dto.Paises         = ParsePaises(wb);
        dto.AccountsGroups = ParseAccountsGroups(wb);
        dto.Verticales     = ParseVerticales(wb);
        dto.Areas          = ParseAreas(wb);

        return Task.FromResult(dto);
    }

    private IXLWorksheet? FindSheet(XLWorkbook wb, string nombreParcial) =>
        wb.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains(nombreParcial, StringComparison.OrdinalIgnoreCase));

    private List<IndustriaReferenciaDto> ParseIndustrias(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "Industria");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'Industria' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));
        if (!colMap.TryGetValue("cod_industria", out int colCod) ||
            !colMap.TryGetValue("verticales", out int colVer))
        {
            _logger.LogWarning("Maestro Industria: columnas requeridas no encontradas.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new IndustriaReferenciaDto
        {
            CodIndustria = ExcelParserHelper.GetString(fila, colCod),
            Vertical     = ExcelParserHelper.GetString(fila, colVer),
        }).Where(r => !string.IsNullOrWhiteSpace(r.CodIndustria)).ToList();
    }

    private List<CeBeReferenciaDto> ParseCeBes(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "CeBe");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'CeBe' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));
        if (!colMap.TryGetValue("cebegroup", out int colGrp) ||
            !colMap.TryGetValue("cebe", out int colCebe) ||
            !colMap.TryGetValue("nombre", out int colNombre))
        {
            _logger.LogWarning("Maestro CeBe: columnas requeridas no encontradas.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new CeBeReferenciaDto
        {
            CeBeGroup = ExcelParserHelper.GetString(fila, colGrp),
            CeBe      = ExcelParserHelper.GetString(fila, colCebe),
            Nombre    = ExcelParserHelper.GetString(fila, colNombre),
        }).Where(r => !string.IsNullOrWhiteSpace(r.CeBe)).ToList();
    }

    private List<SociedadReferenciaDto> ParseSociedades(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "Sociedad");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'Sociedad' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));
        if (!colMap.TryGetValue("sociedad", out int colSoc) ||
            !colMap.TryGetValue("razonsocial", out int colRazon) ||
            !colMap.TryGetValue("pais", out int colPais))
        {
            _logger.LogWarning("Maestro Sociedad: columnas requeridas no encontradas.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new SociedadReferenciaDto
        {
            Sociedad    = ExcelParserHelper.GetString(fila, colSoc),
            RazonSocial = ExcelParserHelper.GetString(fila, colRazon),
            Pais        = ExcelParserHelper.GetString(fila, colPais),
        }).Where(r => !string.IsNullOrWhiteSpace(r.Sociedad)).ToList();
    }

    private List<PaisReferenciaDto> ParsePaises(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "Pais");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'Pais' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));
        if (!colMap.TryGetValue("iso code", out int colISO) ||
            !colMap.TryGetValue("pais", out int colPais))
        {
            _logger.LogWarning("Maestro Pais: columnas requeridas no encontradas.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new PaisReferenciaDto
        {
            ISOCode = ExcelParserHelper.GetString(fila, colISO),
            Pais    = ExcelParserHelper.GetString(fila, colPais),
        }).Where(r => !string.IsNullOrWhiteSpace(r.ISOCode)).ToList();
    }

    private List<AccountsGroupReferenciaDto> ParseAccountsGroups(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "Accounts_Group");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'Accounts_Group' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));
        if (!colMap.TryGetValue("lineitemid", out int colLine) ||
            !colMap.TryGetValue("account", out int colAcc) ||
            !colMap.TryGetValue("clasificacion", out int colClasif))
        {
            _logger.LogWarning("Maestro Accounts_Group: columnas requeridas no encontradas.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new AccountsGroupReferenciaDto
        {
            LineItemId    = ExcelParserHelper.GetString(fila, colLine),
            Account       = ExcelParserHelper.GetString(fila, colAcc),
            Clasificacion = ExcelParserHelper.GetString(fila, colClasif),
        }).Where(r => !string.IsNullOrWhiteSpace(r.LineItemId)).ToList();
    }

    private List<VerticalReferenciaDto> ParseVerticales(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "Verticales");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'Verticales' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));
        if (!colMap.TryGetValue("verticales", out int colVer) ||
            !colMap.TryGetValue("cod industria", out int colCod))
        {
            _logger.LogWarning("Maestro Verticales: columnas requeridas no encontradas.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new VerticalReferenciaDto
        {
            Vertical     = ExcelParserHelper.GetString(fila, colVer),
            CodIndustria = ExcelParserHelper.GetString(fila, colCod),
        }).Where(r => !string.IsNullOrWhiteSpace(r.Vertical)).ToList();
    }

    private List<AreaReferenciaDto> ParseAreas(XLWorkbook wb)
    {
        var ws = FindSheet(wb, "Area");
        if (ws is null) { _logger.LogWarning("Maestro: hoja 'Area' no encontrada."); return []; }

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));

        // Columna "VEU / Area" normaliza a "veu / area"
        var colArea = colMap.FirstOrDefault(kv =>
            kv.Key.Contains("area", StringComparison.OrdinalIgnoreCase)).Value;
        colMap.TryGetValue("cebe", out int colCebe);

        if (colArea == 0)
        {
            _logger.LogWarning("Maestro Area: columna de área no encontrada.");
            return [];
        }

        return ws.RowsUsed().Skip(1).Select(fila => new AreaReferenciaDto
        {
            Area = ExcelParserHelper.GetString(fila, colArea),
            CeBe = ExcelParserHelper.GetString(fila, colCebe),
        }).Where(r => !string.IsNullOrWhiteSpace(r.Area)).ToList();
    }
}
