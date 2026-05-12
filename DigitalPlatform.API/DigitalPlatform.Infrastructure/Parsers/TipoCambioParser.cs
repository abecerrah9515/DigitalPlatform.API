using ClosedXML.Excel;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

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

        if (!colMap.TryGetValue("ano", out int colAno) ||
            !colMap.TryGetValue("mes", out int colMes) ||
            !colMap.TryGetValue("tarifa", out int colTarifa))
        {
            _logger.LogWarning("TDC: columnas requeridas no encontradas. Columnas encontradas: {Cols}",
                string.Join(", ", colMap.Keys));
            return Task.FromResult(resultado);
        }

        foreach (var fila in ws.RowsUsed().Skip(1))
        {
            try
            {
                resultado.Add(new RegistroTipoCambioDto
                {
                    Año    = ExcelParserHelper.GetInt(fila, colAno),
                    Mes    = ExcelParserHelper.GetInt(fila, colMes),
                    Tarifa = ExcelParserHelper.GetDecimal(fila, colTarifa),
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
