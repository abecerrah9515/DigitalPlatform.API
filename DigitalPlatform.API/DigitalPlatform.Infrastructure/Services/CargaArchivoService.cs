using System.Globalization;
using DigitalPlatform.Application.Common;
using DigitalPlatform.Application.DTOs.Cartera;
using DigitalPlatform.Application.Interfaces;
using DigitalPlatform.Domain.Entities;
using DigitalPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace DigitalPlatform.Infrastructure.Services;

public class CargaArchivoService : ICargaArchivoService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CargaArchivoService> _logger;

    public CargaArchivoService(ApplicationDbContext db, ILogger<CargaArchivoService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CargaArchivoDto>>> ObtenerHistorialAsync()
    {
        var historial = await _db.CargasArchivo
            .OrderByDescending(c => c.FechaCarga)
            .Select(c => new CargaArchivoDto
            {
                Id = c.Id,
                Tipo = c.Tipo,
                NombreArchivo = c.NombreArchivo,
                FechaCarga = c.FechaCarga,
                TotalRegistros = c.TotalRegistros,
            })
            .ToListAsync();

        return ApiResponse<List<CargaArchivoDto>>.Ok(historial);
    }

    public async Task<ApiResponse<string>> SubirArchivoAsync(string tipo, Stream fileStream, string fileName)
    {
        try
        {
            if (tipo == "control-facturas" || tipo == "base-clientes" || tipo == "reporte-cartera")
            {
                var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                ms.Position = 0;
                var todasLasFilas = await LeerTodasLasHojasSinEncabezadosAsync(ms);
                return await GuardarAsync(tipo, fileName, todasLasFilas);
            }

            var rows = await LeerExcelAsync(fileStream);
            return await GuardarAsync(tipo, fileName, rows);
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error al procesar archivo {Tipo}: {File}. Detalle: {Error}", tipo, fileName, fullError);
            return ApiResponse<string>.Fail($"Error al procesar el archivo: {fullError}");
        }
    }

    private async Task<ApiResponse<string>> GuardarAsync(string tipo, string fileName, List<Dictionary<string, object>> rows)
    {
        try
        {
            var existentes = await _db.CargasArchivo.Where(c => c.Tipo == tipo).ToListAsync();
            if (existentes.Count > 0)
            {
                _db.CargasArchivo.RemoveRange(existentes);
                await _db.SaveChangesAsync();
            }

            var carga = new CargaArchivo
            {
                Tipo = tipo,
                NombreArchivo = fileName,
                FechaCarga = DateTime.UtcNow,
            };

            _db.CargasArchivo.Add(carga);
            await _db.SaveChangesAsync();

            int totalRegistros = 0;
            switch (tipo)
            {
                case "base-clientes":
                    totalRegistros = GuardarBaseClientes(rows, carga.Id);
                    break;
                case "control-facturas":
                    totalRegistros = GuardarControlFacturas(rows, carga.Id);
                    break;
                case "reporte-cartera":
                    totalRegistros = GuardarReporteCartera(rows, carga.Id);
                    break;
                default:
                    return ApiResponse<string>.Fail($"Tipo de archivo no soportado: {tipo}");
            }

            carga.TotalRegistros = totalRegistros;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Archivo {Tipo} procesado: {N} registros", tipo, totalRegistros);

            return ApiResponse<string>.Ok($"Archivo {tipo} cargado exitosamente ({totalRegistros} registros).");
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error al procesar archivo {Tipo}: {File}. Detalle: {Error}", tipo, fileName, fullError);
            return ApiResponse<string>.Fail($"Error al procesar el archivo: {fullError}");
        }
    }

    private static async Task<List<Dictionary<string, object>>> LeerExcelAsync(Stream stream)
    {
        var result = new List<Dictionary<string, object>>();
        var rows = await stream.QueryAsync(useHeaderRow: true);
        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> dict)
                result.Add(new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase));
        }
        return result;
    }

    private static async Task<List<Dictionary<string, object>>> LeerExcelSinEncabezadosAsync(Stream stream)
    {
        var result = new List<Dictionary<string, object>>();
        var rows = await stream.QueryAsync(useHeaderRow: false);
        foreach (var row in rows)
        {
            if (row is IDictionary<string, object> dict)
                result.Add(new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase));
        }
        return result;
    }

    private static async Task<List<Dictionary<string, object>>> LeerTodasLasHojasSinEncabezadosAsync(MemoryStream ms)
    {
        var result = new List<Dictionary<string, object>>();
        ms.Position = 0;
        var sheetNames = MiniExcel.GetSheetNames(ms).ToList();
        foreach (var sheet in sheetNames)
        {
            ms.Position = 0;
            var rows = await ms.QueryAsync(useHeaderRow: false, sheetName: sheet);
            bool primera = true;
            foreach (var row in rows)
            {
                if (primera) { primera = false; continue; }
                if (row is IDictionary<string, object> dict)
                    result.Add(new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase));
            }
        }
        return result;
    }

    private int GuardarBaseClientes(List<Dictionary<string, object>> rows, int cargaArchivoId)
    {
        var entities = rows.Select(row => new BaseCliente
        {
            CargaArchivoId = cargaArchivoId,
            GrupoCuenta = GetStr(row, ["A"]),
            NumeroCuenta = GetStr(row, ["B"]),
            NombreCliente = GetStr(row, ["C"]),
            CampoClas = GetStr(row, ["D"]),
            Calle = GetStr(row, ["E"]),
            NIT = GetStr(row, ["J"]),
            NombreContacto = GetStr(row, ["AI"]),
            ContactoContabilidad = GetStr(row, ["AO"]),
            ContactoTesoreria = GetStr(row, ["AP"]),
            ContactoFinanzas = GetStr(row, ["AQ"]),
            ContactoOperacion = GetStr(row, ["AR"]),
            ContactoComercial = GetStr(row, ["AS"]),
            ContactoCompras = GetStr(row, ["AT"]),
            Telefono = GetStr(row, ["AU"]),
            CorreoContabilidad = GetStr(row, ["AV"]),
            Pais = GetStr(row, ["AZ"]),
        }).ToList();

        _db.BaseClientes.AddRange(entities);
        return entities.Count;
    }

    private int GuardarControlFacturas(List<Dictionary<string, object>> rows, int cargaArchivoId)
    {
        var entities = rows.Select(row => new ControlFactura
        {
            CargaArchivoId = cargaArchivoId,
            FechaSolicito = GetDate(row, ["A"]),
            FechaEmision = GetDate(row, ["B"]),
            FechaEnvio = GetDate(row, ["C"]),
            FechaPago = GetDate(row, ["D"]),
            NumeroCliente = GetStr(row, ["E"]),
            Pep = GetStr(row, ["F"]),
            Cliente = GetStr(row, ["G"]),
            Valor = GetDec(row, ["H"]),
            Iva = GetDec(row, ["I"]),
            ReteIva = GetDec(row, ["J"]),
            Autorenta = GetDec(row, ["K"]),
            Retencion = GetDec(row, ["L"]),
            Ica = GetDec(row, ["M"]),
            Total = GetDec(row, ["N"]),
            OrdenConsecutivo = GetStr(row, ["O"]),
            NumeroDocumento = GetStr(row, ["P"]),
            OrdenPedido = GetStr(row, ["Q"]),
            EntradaMercancia = GetStr(row, ["R"]),
            Concepto = GetStr(row, ["S"]),
            Observaciones = GetStr(row, ["T"]),
            Estado = GetStr(row, ["U"]),
            DatosAdicionales = GetStr(row, ["V"]),
            Trm = GetDec(row, ["W"]),
            DiaTrm = GetDate(row, ["X"]),
            ValorUsd = GetDec(row, ["Y"]),
            FechaSolicitud = GetDate(row, ["Z"]),
            ValorAnulacion = GetDec(row, ["AA"]),
            ServicioProducto = GetStr(row, ["AB"]),
            RazonAnulacion = GetStr(row, ["AC"]),
            NotaCredito = GetStr(row, ["AD"]),
            NumeroDocumento2 = GetStr(row, ["AE"]),
            Compensacion = GetStr(row, ["AF"]),
            Reemplazo = GetStr(row, ["AG"]),
            ValorCancelar = GetDec(row, ["AH"]),
            ActaNumero = GetStr(row, ["AI"]),
            NumeroSeguimiento = GetStr(row, ["AJ"]),
        }).ToList();

        _db.ControlFacturas.AddRange(entities);
        return entities.Count;
    }

    private int GuardarReporteCartera(List<Dictionary<string, object>> rows, int cargaArchivoId)
    {
        var entities = rows.Select(row => new ReporteCarteraFactura
        {
            CargaArchivoId = cargaArchivoId,
            CuentaMayor = GetStr(row, ["A"]),
            Deudor = GetStr(row, ["B"]),
            RazonSocial = GetStr(row, ["C"]),
            Responsable = GetStr(row, ["D"]),
            Asignacion = GetStr(row, ["E"]),
            Referencia = GetStr(row, ["F"]),
            FechaDocumento = GetDate(row, ["G"]),
            FechaContabiliz = GetDate(row, ["H"]),
            FechaPago = GetDate(row, ["I"]),
            Comentario = GetStr(row, ["J"]),
            FechaCompromiso = GetStr(row, ["K"]),
            FechaPagoReal = GetDate(row, ["L"]),
            AcuerdoPago = GetStr(row, ["M"]),
            Semana = GetStr(row, ["N"]),
            ImporteMonedaLocal = GetDec(row, ["O"]),
            ImporteMonedaDoc = GetDec(row, ["P"]),
            ValorRecibir = GetDec(row, ["Q"]),
            MonedaDocumento = GetStr(row, ["R"]),
            DemoraDPP1 = GetInt(row, ["S"]),
            Estado = GetStr(row, ["T"]),
            VencidoEnTiempo = GetDec(row, ["U"]),
            Vencido0_15 = GetDec(row, ["V"]),
            Vencido16_30 = GetDec(row, ["W"]),
            Vencido31_60 = GetDec(row, ["X"]),
            Vencido61_90 = GetDec(row, ["Y"]),
            Vencido91_120 = GetDec(row, ["Z"]),
            Vencido121_365 = GetDec(row, ["AA"]),
        }).ToList();

        _db.ReporteCarteraFacturas.AddRange(entities);
        return entities.Count;
    }

    private static string GetStr(Dictionary<string, object> row, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (row.TryGetValue(key, out var v) && v is not null)
            {
                var s = v.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
        }
        return string.Empty;
    }

    private static decimal GetDec(Dictionary<string, object> row, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (!row.TryGetValue(key, out var v) || v is null) continue;
            decimal val = v switch
            {
                decimal d => d,
                double db => (decimal)db,
                int i => i,
                long l => l,
                string s => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m,
                _ => 0m
            };
            if (val != 0m) return val;
        }
        return 0m;
    }

    private static DateTime? GetDate(Dictionary<string, object> row, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (!row.TryGetValue(key, out var v) || v is null) continue;
            try
            {
                var val = v switch
                {
                    DateTime dt => dt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                        : dt.ToUniversalTime(),
                    double d when d > 0 && d < 100000 => DateTime.FromOADate(d).ToUniversalTime(),
                    string s when DateTime.TryParse(s, out var dt) => dt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                        : dt.ToUniversalTime(),
                    _ => (DateTime?)null
                };
                if (val.HasValue) return val;
            }
            catch { }
        }
        return null;
    }

    private static int GetInt(Dictionary<string, object> row, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (!row.TryGetValue(key, out var v) || v is null) continue;
            int val = v switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                decimal dm => (int)dm,
                string s => int.TryParse(s, out var i) ? i : 0,
                _ => 0
            };
            if (val != 0) return val;
        }
        return 0;
    }
}

