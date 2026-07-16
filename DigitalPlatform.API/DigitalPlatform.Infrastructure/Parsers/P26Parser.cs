using MiniExcelLibs;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

/// <summary>
/// Parser del archivo de plan Arch.P26. Lee la hoja "P26" con el plan por
/// Source.Name (vertical) × Task × Year × MonthNum (HUE-02).
/// </summary>
public class P26Parser : IP26Parser
{
    private const string HojaOrigen = "P26";

    // Columnas mínimas requeridas según HUE-02
    private static readonly string[] _columnasRequeridas =
        ["source.name", "task", "year", "amount", "monthnum"];

    private readonly ILogger<P26Parser> _logger;

    public P26Parser(ILogger<P26Parser> logger) => _logger = logger;

    public Task<List<RegistroP26Dto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null)
    {
        var resultado  = new List<RegistroP26Dto>();
        var sheetNames = archivo.GetSheetNames();

        var hoja = sheetNames.FirstOrDefault(s =>
            s.Equals(HojaOrigen, StringComparison.OrdinalIgnoreCase));

        if (hoja is null)
            throw new InvalidOperationException(
                $"Arch.P26: no se encontró la hoja requerida '{HojaOrigen}'.");

        var filas    = archivo.Query(useHeaderRow: true, sheetName: hoja);
        var validado = false;

        foreach (IDictionary<string, object> fila in filas)
        {
            var row = ExcelParserHelper.NormalizeRow(fila);

            if (!validado)
            {
                validado = true;
                ExcelParserHelper.ValidarColumnas(row.Keys, _columnasRequeridas, "Arch.P26");
            }

            try
            {
                resultado.Add(new RegistroP26Dto
                {
                    SourceName = ExcelParserHelper.GetString(row, "source.name"),
                    Task       = ExcelParserHelper.GetString(row, "task"),
                    Año        = ExcelParserHelper.GetInt(row, "year"),
                    Mes        = ExcelParserHelper.GetInt(row, "monthnum"),
                    Amount     = ExcelParserHelper.GetDecimal(row, "amount"),
                });
                if (resultado.Count % 500 == 0) onProgress?.Invoke(resultado.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "P26: error en fila ignorado.");
            }
        }

        return Task.FromResult(resultado);
    }
}
