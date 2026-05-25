using MiniExcelLibs;
using DigitalPlatform.Application.DTOs.Fuentes;
using DigitalPlatform.Application.Interfaces.Parsers;
using Microsoft.Extensions.Logging;

namespace DigitalPlatform.Infrastructure.Parsers;

// Ayuda a extraer solo el código de campos con formato "CÓDIGO - Descripción" o "Nombre - CÓDIGO"
file static class PlaneacionCampoHelper
{
    // "1-0000034220 - Acerias. Soporte..." → "1-0000034220"
    internal static string ExtraerPrimerSegmento(string valor) =>
        valor.Contains(" - ") ? valor.Split(new[] { " - " }, 2, StringSplitOptions.None)[0].Trim() : valor.Trim();

    // "AMS SAP - 7310106" → "7310106"
    internal static string ExtraerUltimoSegmento(string valor) =>
        valor.Contains(" - ") ? valor.Split(new[] { " - " }, StringSplitOptions.None)[^1].Trim() : valor.Trim();
}

public class PlaneacionParser : IPlaneacionParser
{
    private const string NombreArchivo = "Planeacion.xlsx";
    private const string HojaOrigen   = "qData";

    // Columnas mínimas requeridas según HUE-02
    private static readonly string[] _columnasRequeridas =
    [
        "cliente", "proyecto", "ano", "mes",
        "ingreso_previsto_eur", "coste_previsto_eur",
        "cebe", "industria", "brm", "responsable_wbs"
    ];

    private readonly ILogger<PlaneacionParser> _logger;

    public PlaneacionParser(ILogger<PlaneacionParser> logger) => _logger = logger;

    public Task<List<RegistroPlaneacionDto>> ParsearAsync(Stream archivo, Action<int>? onProgress = null)
    {
        var resultado  = new List<RegistroPlaneacionDto>();
        var sheetNames = archivo.GetSheetNames();

        var hoja = sheetNames.FirstOrDefault(s =>
            s.Equals(HojaOrigen, StringComparison.OrdinalIgnoreCase));

        if (hoja is null)
            throw new InvalidOperationException(
                $"Arch.Planeacion: no se encontró la hoja requerida '{HojaOrigen}'.");

        var filas    = archivo.Query(useHeaderRow: true, sheetName: hoja);
        var validado = false;
        var numFila  = 2;

        foreach (IDictionary<string, object> fila in filas)
        {
            var row = ExcelParserHelper.NormalizeRow(fila);

            if (!validado)
            {
                validado = true;
                ExcelParserHelper.ValidarColumnas(row.Keys, _columnasRequeridas, "Arch.Planeacion");
            }

            try
            {
                // proyecto: "1-0000034220 - Acerias. Soporte..." → "1-0000034220"
                var proyecto = PlaneacionCampoHelper.ExtraerPrimerSegmento(
                    ExcelParserHelper.GetString(row, "proyecto"));

                // cebe: "AMS SAP - 7310106" → "7310106"
                var cebe = PlaneacionCampoHelper.ExtraerUltimoSegmento(
                    ExcelParserHelper.GetString(row, "cebe"));

                // industria: "Z09 - Nat. Res, Energy/Ut" → "Z09"
                var industria = PlaneacionCampoHelper.ExtraerPrimerSegmento(
                    ExcelParserHelper.GetString(row, "industria"));

                resultado.Add(new RegistroPlaneacionDto
                {
                    Cliente            = ExcelParserHelper.GetString(row, "cliente"),
                    Proyecto           = proyecto,
                    Año                = ExcelParserHelper.GetIntRequired(row, "ano"),
                    Mes                = ExcelParserHelper.GetIntRequired(row, "mes"),
                    IngresoPrevistoEur = ExcelParserHelper.GetDecimalRequired(row, "ingreso_previsto_eur"),
                    CostePrevistoEur   = ExcelParserHelper.GetDecimalRequired(row, "coste_previsto_eur"),
                    Cebe               = cebe,
                    Industria          = industria,
                    Brm                = ExcelParserHelper.GetString(row, "brm"),
                    ResponsableWbs     = ExcelParserHelper.GetString(row, "responsable_wbs"),
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
