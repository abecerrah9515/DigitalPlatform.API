using MiniExcelLibs;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

public class PlaneacionParser : IPlaneacionParser
{
    private const string HojaOrigen = "qData";

    private readonly ILogger<PlaneacionParser> _logger;

    public PlaneacionParser(ILogger<PlaneacionParser> logger) => _logger = logger;

    public Task<List<RegistroPlaneacionDto>> ParsearAsync(Stream archivo)
    {
        var resultado  = new List<RegistroPlaneacionDto>();
        var sheetNames = archivo.GetSheetNames();

        var hoja = sheetNames.FirstOrDefault(s =>
            s.Equals(HojaOrigen, StringComparison.OrdinalIgnoreCase));

        if (hoja is null)
        {
            _logger.LogWarning("Planeación: no se encontró la hoja '{Sheet}'.", HojaOrigen);
            return Task.FromResult(resultado);
        }

        var filas    = archivo.Query(useHeaderRow: true, sheetName: hoja);
        var validado = false;
        var omitir   = false;

        foreach (IDictionary<string, object> fila in filas)
        {
            var row = ExcelParserHelper.NormalizeRow(fila);

            if (!validado)
            {
                validado = true;
                if (!row.ContainsKey("cliente") || !row.ContainsKey("proyecto"))
                {
                    _logger.LogWarning("Planeación: columnas requeridas no encontradas en '{Sheet}'.", HojaOrigen);
                    omitir = true;
                }
            }

            if (omitir) break;

            try
            {
                resultado.Add(new RegistroPlaneacionDto
                {
                    Cliente            = ExcelParserHelper.GetString(row, "cliente"),
                    Proyecto           = ExcelParserHelper.GetString(row, "proyecto"),
                    Año                = ExcelParserHelper.GetInt(row, "ano"),
                    Mes                = ExcelParserHelper.GetInt(row, "mes"),
                    IngresoPrevistoEur = ExcelParserHelper.GetDecimal(row, "ingreso_previsto_eur"),
                    CostePrevistoEur   = ExcelParserHelper.GetDecimal(row, "coste_previsto_eur"),
                    Cebe               = ExcelParserHelper.GetString(row, "cebe"),
                    Industria          = ExcelParserHelper.GetString(row, "industria"),
                    Brm                = ExcelParserHelper.GetString(row, "brm"),
                    ResponsableWbs     = ExcelParserHelper.GetString(row, "responsable_wbs"),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Planeación: error en fila ignorado.");
            }
        }

        return Task.FromResult(resultado);
    }
}
