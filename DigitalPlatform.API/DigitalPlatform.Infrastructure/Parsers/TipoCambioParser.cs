using ClosedXML.Excel;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DigitalPlatform.Infrastructure.Parsers;

public class TipoCambioParser : ITipoCambioParser
{
    private readonly ILogger<TipoCambioParser> _logger;

    public TipoCambioParser(ILogger<TipoCambioParser> logger) => _logger = logger;

    public Task<List<RegistroTipoCambioDto>> ParsearAsync(Stream archivo)
    {
        var resultado = new List<RegistroTipoCambioDto>();

        using var wb = new XLWorkbook(archivo);
        var ws = wb.Worksheets.First();

        var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));

        if (!colMap.TryGetValue("fecha", out int colFecha) ||
            !colMap.TryGetValue("t.c. mxn", out int colPeriodo) ||
            !colMap.TryGetValue("tasas", out int colTasa))
        {
            _logger.LogWarning("TDC: columnas requeridas no encontradas. Columnas encontradas: {Cols}",
                string.Join(", ", colMap.Keys));
            return Task.FromResult(resultado);
        }

        foreach (var fila in ws.RowsUsed().Skip(1))
        {
            try
            {
                var fechaStr   = ExcelParserHelper.GetString(fila, colFecha);
                var periodoStr = ExcelParserHelper.GetString(fila, colPeriodo);
                var tasa       = ExcelParserHelper.GetDecimal(fila, colTasa);

                if (!DateOnly.TryParseExact(fechaStr, "dd.MM.yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
                {
                    _logger.LogWarning("TDC fila {Row}: fecha inválida '{Val}', se omite.", fila.RowNumber(), fechaStr);
                    continue;
                }

                var partes = periodoStr.Split('/');
                if (partes.Length != 2 ||
                    !int.TryParse(partes[0], out int año) ||
                    !int.TryParse(partes[1], out int mes))
                {
                    _logger.LogWarning("TDC fila {Row}: período inválido '{Val}', se omite.", fila.RowNumber(), periodoStr);
                    continue;
                }

                resultado.Add(new RegistroTipoCambioDto
                {
                    Fecha = fecha,
                    Año   = año,
                    Mes   = mes,
                    Tasa  = tasa,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TDC fila {Row}: error ignorado.", fila.RowNumber());
            }
        }

        return Task.FromResult(resultado);
    }
}
