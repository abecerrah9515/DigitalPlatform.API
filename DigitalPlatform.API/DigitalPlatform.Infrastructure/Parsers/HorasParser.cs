using MiniExcelLibs;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

public class HorasParser : IHorasParser
{
    private const string NombreArchivo  = "Horas.xlsx";
    private const string HojaOrigen    = "qData";
    private const string EstadoAceptado = "Accepted";

    // Columnas mínimas requeridas según HUG-02
    private static readonly string[] _columnasRequeridas =
    [
        "trabajador_id_softtek", "trabajador_nombre", "trabajador_ceco",
        "proyecto", "proyecto_sociedad_fi", "proyecto_industria",
        "proyecto_engagement_leader",
        "ano", "mes", "horas", "estado", "brm"
    ];

    private readonly ILogger<HorasParser> _logger;

    public HorasParser(ILogger<HorasParser> logger) => _logger = logger;

    public Task<List<RegistroHorasDto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null)
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
        var numFila  = 2;

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
                {
                    numFila++;
                    continue;
                }

                // proyecto: "1-0000034220 - Nombre..." → "1-0000034220"
                var rawProyecto = ExcelParserHelper.GetString(row, "proyecto");
                var proyecto = rawProyecto.Contains(" - ")
                    ? rawProyecto.Split(new[] { " - " }, 2, StringSplitOptions.None)[0].Trim()
                    : rawProyecto.Trim();

                // proyecto_area: "90702 - AMS" → "AMS"; "---" / " - " → vacío
                var rawArea = ExcelParserHelper.GetString(row, "proyecto_area");
                var area = rawArea.Contains(" - ")
                    ? rawArea.Split(new[] { " - " }, 2, StringSplitOptions.None)[^1].Trim()
                    : string.Empty;
                if (area is "---" or "-") area = string.Empty;

                resultado.Add(new RegistroHorasDto
                {
                    TrabajadorId = ExcelParserHelper.GetString(row, "trabajador_id_softtek"),
                    Nombre       = ExcelParserHelper.GetString(row, "trabajador_nombre"),
                    Ceco         = ExcelParserHelper.GetString(row, "trabajador_ceco"),
                    Proyecto     = proyecto,
                    Sociedad     = ExcelParserHelper.GetString(row, "proyecto_sociedad_fi"),
                    Industria    = ExcelParserHelper.GetString(row, "proyecto_industria"),
                    Area         = area,
                    Año          = ExcelParserHelper.GetIntRequired(row, "ano"),
                    Mes          = ExcelParserHelper.GetIntRequired(row, "mes"),
                    Horas        = ExcelParserHelper.GetDecimalRequired(row, "horas"),
                    Brm          = ExcelParserHelper.GetString(row, "brm"),
                });
                if (resultado.Count % 100 == 0) onProgress?.Invoke(resultado.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{Archivo} — hoja '{Sheet}', fila {Fila}: {Mensaje}",
                    NombreArchivo, HojaOrigen, numFila, ex.Message);
            }
            finally
            {
                numFila++;
            }
        }

        return Task.FromResult(resultado);
    }
}
