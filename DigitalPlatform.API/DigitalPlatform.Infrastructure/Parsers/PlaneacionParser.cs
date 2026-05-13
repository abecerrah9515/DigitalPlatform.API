using ClosedXML.Excel;
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
        var resultado = new List<RegistroPlaneacionDto>();

        using var wb = new XLWorkbook(archivo);

        if (!wb.TryGetWorksheet(HojaOrigen, out var ws))
        {
            _logger.LogWarning("Planeación: no se encontró la hoja '{Sheet}'.", HojaOrigen);
            return Task.FromResult(resultado);
        }

        var colMap = ExcelParserHelper.BuildColumnMap(ws!.Row(1));

        if (!colMap.TryGetValue("cliente", out int colCliente) ||
            !colMap.TryGetValue("proyecto", out int colProyecto))
        {
            _logger.LogWarning("Planeación: columnas requeridas no encontradas en '{Sheet}'.", HojaOrigen);
            return Task.FromResult(resultado);
        }

        colMap.TryGetValue("ano", out int colAno);
        colMap.TryGetValue("mes", out int colMes);
        colMap.TryGetValue("ingreso_previsto_eur", out int colIngreso);
        colMap.TryGetValue("coste_previsto_eur", out int colCoste);
        colMap.TryGetValue("cebe", out int colCebe);
        colMap.TryGetValue("industria", out int colIndustria);
        colMap.TryGetValue("brm", out int colBrm);
        colMap.TryGetValue("responsable_wbs", out int colResponsable);

        foreach (var fila in ws.RowsUsed().Skip(1))
        {
            try
            {
                resultado.Add(new RegistroPlaneacionDto
                {
                    Cliente            = ExcelParserHelper.GetString(fila, colCliente),
                    Proyecto           = ExcelParserHelper.GetString(fila, colProyecto),
                    Año                = ExcelParserHelper.GetInt(fila, colAno),
                    Mes                = ExcelParserHelper.GetInt(fila, colMes),
                    IngresoPrevistoEur = ExcelParserHelper.GetDecimal(fila, colIngreso),
                    CostePrevistoEur   = ExcelParserHelper.GetDecimal(fila, colCoste),
                    Cebe               = ExcelParserHelper.GetString(fila, colCebe),
                    Industria          = ExcelParserHelper.GetString(fila, colIndustria),
                    Brm                = ExcelParserHelper.GetString(fila, colBrm),
                    ResponsableWbs     = ExcelParserHelper.GetString(fila, colResponsable),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Planeación fila {Row}: error ignorado.", fila.RowNumber());
            }
        }

        return Task.FromResult(resultado);
    }
}
