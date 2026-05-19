using MiniExcelLibs;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

public class MaestroReferenciasParser : IMaestroReferenciasParser
{
    private readonly ILogger<MaestroReferenciasParser> _logger;

    public MaestroReferenciasParser(ILogger<MaestroReferenciasParser> logger) => _logger = logger;

    public Task<MaestroReferenciasDto> ParsearAsync(Stream archivo, Action<int>? onProgress = null)
    {
        var dto        = new MaestroReferenciasDto();
        var sheetNames = archivo.GetSheetNames();
        var totalFilas = 0;

        dto.Industrias     = ParseSheet(archivo, sheetNames, "Industria",      ParseIndustria,     ref totalFilas, onProgress);
        dto.CeBes          = ParseSheet(archivo, sheetNames, "CeBe",           ParseCeBe,          ref totalFilas, onProgress);
        dto.Sociedades     = ParseSheet(archivo, sheetNames, "Sociedad",        ParseSociedad,      ref totalFilas, onProgress);
        dto.Paises         = ParseSheet(archivo, sheetNames, "Pais",            ParsePais,          ref totalFilas, onProgress);
        dto.AccountsGroups = ParseSheet(archivo, sheetNames, "Accounts_Group",  ParseAccountsGroup, ref totalFilas, onProgress);
        dto.Verticales     = ParseSheet(archivo, sheetNames, "Verticales",      ParseVertical,      ref totalFilas, onProgress);
        dto.Areas          = ParseSheet(archivo, sheetNames, "Area",            ParseArea,          ref totalFilas, onProgress);

        return Task.FromResult(dto);
    }

    private List<T> ParseSheet<T>(
        Stream archivo,
        IReadOnlyList<string> sheetNames,
        string nombreParcial,
        Func<Dictionary<string, object?>, T?> parseRow,
        ref int totalFilas,
        Action<int>? onProgress)
    {
        var hoja = sheetNames.FirstOrDefault(s =>
            s.Contains(nombreParcial, StringComparison.OrdinalIgnoreCase));

        if (hoja is null)
        {
            _logger.LogWarning("Maestro: hoja '{Sheet}' no encontrada.", nombreParcial);
            return [];
        }

        var resultado = new List<T>();
        foreach (IDictionary<string, object> fila in archivo.Query(useHeaderRow: true, sheetName: hoja))
        {
            var row = ExcelParserHelper.NormalizeRow(fila);
            try
            {
                var item = parseRow(row);
                if (item is not null) resultado.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Maestro hoja '{Sheet}': error en fila ignorado.", hoja);
            }
            totalFilas++;
            if (totalFilas % 100 == 0) onProgress?.Invoke(totalFilas);
        }
        return resultado;
    }

    private IndustriaReferenciaDto? ParseIndustria(Dictionary<string, object?> row)
    {
        var cod = ExcelParserHelper.GetString(row, "cod_industria");
        return string.IsNullOrWhiteSpace(cod) ? null : new IndustriaReferenciaDto
        {
            CodIndustria = cod,
            Vertical     = ExcelParserHelper.GetString(row, "verticales"),
        };
    }

    private CeBeReferenciaDto? ParseCeBe(Dictionary<string, object?> row)
    {
        var cebe = ExcelParserHelper.GetString(row, "cebe");
        return string.IsNullOrWhiteSpace(cebe) ? null : new CeBeReferenciaDto
        {
            CeBeGroup = ExcelParserHelper.GetString(row, "cebegroup"),
            CeBe      = cebe,
            Nombre    = ExcelParserHelper.GetString(row, "nombre"),
        };
    }

    private SociedadReferenciaDto? ParseSociedad(Dictionary<string, object?> row)
    {
        var soc = ExcelParserHelper.GetString(row, "sociedad");
        return string.IsNullOrWhiteSpace(soc) ? null : new SociedadReferenciaDto
        {
            Sociedad    = soc,
            RazonSocial = ExcelParserHelper.GetString(row, "razonsocial"),
            Pais        = ExcelParserHelper.GetString(row, "pais"),
        };
    }

    private PaisReferenciaDto? ParsePais(Dictionary<string, object?> row)
    {
        var iso = ExcelParserHelper.GetString(row, "iso code");
        return string.IsNullOrWhiteSpace(iso) ? null : new PaisReferenciaDto
        {
            ISOCode = iso,
            Pais    = ExcelParserHelper.GetString(row, "pais"),
        };
    }

    private AccountsGroupReferenciaDto? ParseAccountsGroup(Dictionary<string, object?> row)
    {
        var account = ExcelParserHelper.GetString(row, "account");
        return string.IsNullOrWhiteSpace(account) ? null : new AccountsGroupReferenciaDto
        {
            Account       = account,
            LineItemId    = ExcelParserHelper.GetString(row, "lineitemid"),
            Clasificacion = ExcelParserHelper.GetString(row, "clasificacion"),
        };
    }

    private VerticalReferenciaDto? ParseVertical(Dictionary<string, object?> row)
    {
        var vertical = ExcelParserHelper.GetString(row, "verticales");
        return string.IsNullOrWhiteSpace(vertical) ? null : new VerticalReferenciaDto
        {
            Vertical     = vertical,
            CodIndustria = ExcelParserHelper.GetString(row, "cod industria"),
        };
    }

    private AreaReferenciaDto? ParseArea(Dictionary<string, object?> row)
    {
        // Columna puede llamarse "VEU / Area" o similar — buscamos la que contenga "area"
        var areaKey = row.Keys.FirstOrDefault(k =>
            k.Contains("area", StringComparison.OrdinalIgnoreCase));

        var area = areaKey is not null
            ? ExcelParserHelper.GetString(row, areaKey)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(area)) return null;

        // CeBe puede venir como "7310106 - AMS SAP" — extraemos solo el código
        var cebeRaw = ExcelParserHelper.GetString(row, "cebe");
        var cebe    = cebeRaw.Split('-')[0].Trim();

        return new AreaReferenciaDto { Area = area, CeBe = cebe };
    }
}
