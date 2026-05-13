using ClosedXML.Excel;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

public class HorasParser : IHorasParser
{
    private const string HojaOrigen = "qData";
    private const string EstadoAceptado = "Accepted";

    private readonly ILogger<HorasParser> _logger;

    public HorasParser(ILogger<HorasParser> logger) => _logger = logger;

    public Task<List<RegistroHorasDto>> ParsearAsync(Stream archivo)
    {
        var resultado = new List<RegistroHorasDto>();

        using var wb = new XLWorkbook(archivo);

        if (!wb.TryGetWorksheet(HojaOrigen, out var ws))
        {
            _logger.LogWarning("Horas: no se encontró la hoja '{Sheet}'.", HojaOrigen);
            return Task.FromResult(resultado);
        }

        var colMap = ExcelParserHelper.BuildColumnMap(ws!.Row(1));

        if (!colMap.TryGetValue("estado", out int colEstado) ||
            !colMap.TryGetValue("horas", out int colHoras))
        {
            _logger.LogWarning("Horas: columnas requeridas no encontradas en '{Sheet}'.", HojaOrigen);
            return Task.FromResult(resultado);
        }

        colMap.TryGetValue("trabajador_id", out int colId);
        colMap.TryGetValue("trabajador_nombre", out int colNombre);
        colMap.TryGetValue("trabajador_ceco", out int colCeco);
        colMap.TryGetValue("proyecto", out int colProyecto);
        colMap.TryGetValue("trabajador_sociedad_fi", out int colSociedad);
        colMap.TryGetValue("proyecto_industria", out int colIndustria);
        colMap.TryGetValue("ano", out int colAno);
        colMap.TryGetValue("mes", out int colMes);
        colMap.TryGetValue("brm", out int colBrm);

        foreach (var fila in ws.RowsUsed().Skip(1))
        {
            try
            {
                var estado = ExcelParserHelper.GetString(fila, colEstado);
                if (!estado.Equals(EstadoAceptado, StringComparison.OrdinalIgnoreCase))
                    continue;

                resultado.Add(new RegistroHorasDto
                {
                    TrabajadorId = ExcelParserHelper.GetString(fila, colId),
                    Nombre       = ExcelParserHelper.GetString(fila, colNombre),
                    Ceco         = ExcelParserHelper.GetString(fila, colCeco),
                    Proyecto     = ExcelParserHelper.GetString(fila, colProyecto),
                    Sociedad     = ExcelParserHelper.GetString(fila, colSociedad),
                    Industria    = ExcelParserHelper.GetString(fila, colIndustria),
                    Año          = ExcelParserHelper.GetInt(fila, colAno),
                    Mes          = ExcelParserHelper.GetInt(fila, colMes),
                    Horas        = ExcelParserHelper.GetDecimal(fila, colHoras),
                    Brm          = ExcelParserHelper.GetString(fila, colBrm),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Horas fila {Row}: error ignorado.", fila.RowNumber());
            }
        }

        return Task.FromResult(resultado);
    }
}
