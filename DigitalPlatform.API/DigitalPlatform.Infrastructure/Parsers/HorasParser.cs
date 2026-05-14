using MiniExcelLibs;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

public class HorasParser : IHorasParser
{
    private const string HojaOrigen    = "qData";
    private const string EstadoAceptado = "Accepted";

    // Columnas mínimas requeridas según HUE-02
    private static readonly string[] _columnasRequeridas =
    [
        "trabajador_id", "trabajador_nombre", "trabajador_ceco",
        "proyecto", "trabajador_sociedad_fi", "proyecto_industria",
        "ano", "mes", "horas", "estado", "brm"
    ];

    private readonly ILogger<HorasParser> _logger;

    public HorasParser(ILogger<HorasParser> logger) => _logger = logger;

    public Task<List<RegistroHorasDto>> ParsearAsync(Stream archivo)
    {
        var resultado  = new List<RegistroHorasDto>();
        var sheetNames = archivo.GetSheetNames();

        var hoja = sheetNames.FirstOrDefault(s =>
            s.Equals(HojaOrigen, StringComparison.OrdinalIgnoreCase));

        if (hoja is null)
            throw new InvalidOperationException(
                $"Arch.Horas: no se encontró la hoja requerida '{HojaOrigen}'.");

        var filas    = archivo.Query(useHeaderRow: true, sheetName: hoja);
        var validado = false;

        foreach (IDictionary<string, object> fila in filas)
        {
            var row = ExcelParserHelper.NormalizeRow(fila);

            if (!validado)
            {
                validado = true;
                ExcelParserHelper.ValidarColumnas(row.Keys, _columnasRequeridas, "Arch.Horas");
            }

            try
            {
                var estado = ExcelParserHelper.GetString(row, "estado");
                if (!estado.Equals(EstadoAceptado, StringComparison.OrdinalIgnoreCase))
                    continue;

                resultado.Add(new RegistroHorasDto
                {
                    TrabajadorId = ExcelParserHelper.GetString(row, "trabajador_id"),
                    Nombre       = ExcelParserHelper.GetString(row, "trabajador_nombre"),
                    Ceco         = ExcelParserHelper.GetString(row, "trabajador_ceco"),
                    Proyecto     = ExcelParserHelper.GetString(row, "proyecto"),
                    Sociedad     = ExcelParserHelper.GetString(row, "trabajador_sociedad_fi"),
                    Industria    = ExcelParserHelper.GetString(row, "proyecto_industria"),
                    Año          = ExcelParserHelper.GetInt(row, "ano"),
                    Mes          = ExcelParserHelper.GetInt(row, "mes"),
                    Horas        = ExcelParserHelper.GetDecimal(row, "horas"),
                    Brm          = ExcelParserHelper.GetString(row, "brm"),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Horas: error en fila ignorado.");
            }
        }

        return Task.FromResult(resultado);
    }
}
