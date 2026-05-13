using MiniExcelLibs;
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
        var sheetNames = archivo.GetSheetNames();

        foreach (var sheetName in sheetNames)
        {
            var filas = archivo.Query(useHeaderRow: true, sheetName: sheetName);
            var validado = false;
            var omitir   = false;

            foreach (IDictionary<string, object> fila in filas)
            {
                var row = ExcelParserHelper.NormalizeRow(fila);

                if (!validado)
                {
                    validado = true;
                    if (!row.ContainsKey("elemento pep") || !row.ContainsKey("texto"))
                    {
                        _logger.LogWarning("GR55 hoja '{Sheet}': columnas requeridas no encontradas, se omite.", sheetName);
                        omitir = true;
                    }
                }

                if (omitir) break;

                try
                {
                    var elementoPep = ExcelParserHelper.GetString(row, "elemento pep");
                    var texto       = ExcelParserHelper.GetString(row, "texto");

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
                        SocReceptora         = ExcelParserHelper.GetString(row, "soc.receptora"),
                        PeriodoContable      = ExcelParserHelper.GetInt(row, "periodo contable"),
                        Ejercicio            = ExcelParserHelper.GetInt(row, "ejercicio"),
                        NumeroCuenta         = ExcelParserHelper.GetString(row, "numero de cuenta"),
                        Denominacion         = ExcelParserHelper.GetString(row, "denominacion"),
                        ElementoPEP          = elementoPep,
                        CentroBeneficio      = ExcelParserHelper.GetString(row, "centro de beneficio"),
                        Texto                = texto,
                        ValorMonedaLocalCeBe = ExcelParserHelper.GetDecimal(row, "en moneda local centro de beneficio") * -1,
                        ClaveMonedaLocalCeBe = ExcelParserHelper.GetString(row, "clave moneda ml cebe"),
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GR55 hoja '{Sheet}': error en fila ignorado.", sheetName);
                }
            }
        }

        return Task.FromResult(resultado);
    }
}
