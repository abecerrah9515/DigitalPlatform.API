using MiniExcelLibs;
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
        var resultado  = new List<RegistroTipoCambioDto>();
        var sheetNames = archivo.GetSheetNames();
        var primerHoja = sheetNames.FirstOrDefault();

        if (primerHoja is null)
        {
            _logger.LogWarning("TDC: el archivo no contiene hojas.");
            return Task.FromResult(resultado);
        }

        // Columnas mínimas requeridas según HUE-02: Fecha, T.C.MXN, USD, COP
        string[] columnasRequeridas = ["fecha", "t.c. mxn", "usd", "cop"];

        var filas    = archivo.Query(useHeaderRow: true, sheetName: primerHoja);
        var validado = false;

        foreach (IDictionary<string, object> fila in filas)
        {
            var row = ExcelParserHelper.NormalizeRow(fila);

            if (!validado)
            {
                validado = true;
                ExcelParserHelper.ValidarColumnas(row.Keys, columnasRequeridas, "Arch.TDC");
            }

            try
            {
                var fechaVal   = row.TryGetValue("fecha", out var fv) ? fv : null;
                var periodoStr = ExcelParserHelper.GetString(row, "t.c. mxn");

                DateOnly fecha;
                if (fechaVal is DateTime dt)
                {
                    fecha = DateOnly.FromDateTime(dt);
                }
                else
                {
                    var fechaStr = fechaVal?.ToString() ?? "";
                    if (!DateOnly.TryParseExact(fechaStr, "dd.MM.yyyy",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
                    {
                        _logger.LogWarning("TDC: fecha inválida '{Val}', se omite.", fechaStr);
                        continue;
                    }
                }

                var partes = periodoStr.Split('/');
                if (partes.Length != 2 ||
                    !int.TryParse(partes[0], out int año) ||
                    !int.TryParse(partes[1], out int mes))
                {
                    _logger.LogWarning("TDC: período inválido '{Val}', se omite.", periodoStr);
                    continue;
                }

                // Leer tasas disponibles: COP y USD (según HU, TDC tiene columnas COP y USD)
                var tasaCop = ExcelParserHelper.GetDecimal(row, "cop");
                var tasaUsd = ExcelParserHelper.GetDecimal(row, "usd");
                // Fallback a columna genérica "tasas" si existe
                if (tasaCop == 0m) tasaCop = ExcelParserHelper.GetDecimal(row, "tasas");

                resultado.Add(new RegistroTipoCambioDto
                {
                    Fecha   = fecha,
                    Año     = año,
                    Mes     = mes,
                    TasaCop = tasaCop,
                    TasaUsd = tasaUsd,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TDC: error en fila ignorado.");
            }
        }

        return Task.FromResult(resultado);
    }
}
