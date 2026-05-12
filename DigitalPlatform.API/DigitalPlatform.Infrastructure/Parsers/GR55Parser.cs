using ClosedXML.Excel;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DigitalPlatform.Infrastructure.Parsers;

public class GR55Parser : IGR55Parser
{
    private readonly ILogger<GR55Parser> _logger;

    private static readonly Regex PepTokenRegex =
        new(@"(?i)\bPEP\s+([\w\-]+)", RegexOptions.Compiled);

    public GR55Parser(ILogger<GR55Parser> logger) => _logger = logger;

    public Task<List<RegistroGR55Dto>> ParsearAsync(Stream archivo)
    {
        var resultado = new List<RegistroGR55Dto>();

        using var wb = new XLWorkbook(archivo);

        foreach (var ws in wb.Worksheets)
        {
            var colMap = ExcelParserHelper.BuildColumnMap(ws.Row(1));

            if (!colMap.TryGetValue("elemento pep", out int colPep) ||
                !colMap.TryGetValue("texto", out int colTexto))
            {
                _logger.LogWarning("GR55 hoja '{Sheet}': columnas requeridas no encontradas, se omite.", ws.Name);
                continue;
            }

            colMap.TryGetValue("soc.receptora", out int colSoc);
            colMap.TryGetValue("periodo contable", out int colPeriodo);
            colMap.TryGetValue("ejercicio", out int colEjercicio);
            colMap.TryGetValue("numero de cuenta", out int colNumCuenta);
            colMap.TryGetValue("denominacion", out int colDenom);
            colMap.TryGetValue("centro de beneficio", out int colCentro);
            colMap.TryGetValue("en moneda de transaccion", out int colMonto);
            colMap.TryGetValue("clave moneda mt", out int colMoneda);

            foreach (var fila in ws.RowsUsed().Skip(1))
            {
                try
                {
                    var elementoPep = ExcelParserHelper.GetString(fila, colPep);
                    var texto = ExcelParserHelper.GetString(fila, colTexto);

                    if (string.IsNullOrWhiteSpace(elementoPep))
                    {
                        if (!texto.StartsWith("PEP", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var match = PepTokenRegex.Match(texto);
                        if (!match.Success) continue;
                        elementoPep = match.Groups[1].Value;
                    }

                    resultado.Add(new RegistroGR55Dto
                    {
                        SocReceptora        = ExcelParserHelper.GetString(fila, colSoc),
                        PeriodoContable     = ExcelParserHelper.GetInt(fila, colPeriodo),
                        Ejercicio           = ExcelParserHelper.GetInt(fila, colEjercicio),
                        NumeroCuenta        = ExcelParserHelper.GetString(fila, colNumCuenta),
                        Denominacion        = ExcelParserHelper.GetString(fila, colDenom),
                        ElementoPEP         = elementoPep,
                        CentroBeneficio     = ExcelParserHelper.GetString(fila, colCentro),
                        Texto               = texto,
                        EnMonedaTransaccion = ExcelParserHelper.GetDecimal(fila, colMonto),
                        ClaveMonedaMT       = ExcelParserHelper.GetString(fila, colMoneda),
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GR55 hoja '{Sheet}' fila {Row}: error ignorado.", ws.Name, fila.RowNumber());
                }
            }
        }

        return Task.FromResult(resultado);
    }
}
