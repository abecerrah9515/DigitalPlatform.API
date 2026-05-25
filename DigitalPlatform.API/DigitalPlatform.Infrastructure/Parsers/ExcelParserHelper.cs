using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitalPlatform.Infrastructure.Parsers;

internal static class ExcelParserHelper
{
    internal static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return Regex.Replace(sb.ToString().ToLowerInvariant(), @"\s+", " ").Trim();
    }

    // Devuelve un nuevo diccionario con todas las claves normalizadas
    internal static Dictionary<string, object?> NormalizeRow(IDictionary<string, object> row)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in row)
        {
            var key = Normalize(kv.Key ?? "");
            if (!string.IsNullOrWhiteSpace(key))
                result.TryAdd(key, kv.Value);
        }
        return result;
    }

    internal static string GetString(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var val) ? val?.ToString()?.Trim() ?? "" : "";

    internal static int GetInt(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var val)) return 0;
        return val switch
        {
            double d  => (int)d,
            int i     => i,
            long l    => (int)l,
            decimal m => (int)m,
            _         => int.TryParse(val?.ToString(), out var p) ? p : 0
        };
    }

    // Versión estricta: lanza FormatException con nombre de columna si el valor no es convertible.
    // Usar para campos críticos (Año, Mes) donde 0 no es un fallback aceptable.
    internal static int GetIntRequired(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var val) || val is null || val.ToString() == "")
            throw new FormatException($"columna '{key}': valor vacío o ausente.");
        return val switch
        {
            double d  => (int)d,
            int i     => i,
            long l    => (int)l,
            decimal m => (int)m,
            _ => int.TryParse(val.ToString(), out var p)
                 ? p
                 : throw new FormatException($"columna '{key}': valor no numérico '{val}'.")
        };
    }

    internal static decimal GetDecimal(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var val)) return 0m;
        return val switch
        {
            double d  => (decimal)d,
            decimal m => m,
            int i     => i,
            long l    => l,
            _         => decimal.TryParse(val?.ToString(), NumberStyles.Any,
                             CultureInfo.InvariantCulture, out var p) ? p : 0m
        };
    }

    // Versión estricta: lanza FormatException con nombre de columna si el valor no es convertible.
    // Usar para campos monetarios donde un 0 silencioso distorsionaría los totales.
    internal static decimal GetDecimalRequired(Dictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var val) || val is null || val.ToString() == "")
            throw new FormatException($"columna '{key}': valor vacío o ausente.");
        return val switch
        {
            double d  => (decimal)d,
            decimal m => m,
            int i     => i,
            long l    => l,
            _ => decimal.TryParse(val?.ToString(), NumberStyles.Any,
                     CultureInfo.InvariantCulture, out var p)
                 ? p
                 : throw new FormatException($"columna '{key}': valor no numérico '{val}'.")
        };
    }

    // Valida que todas las columnas requeridas estén presentes en la primera fila.
    // Lanza InvalidOperationException con la lista de columnas faltantes si alguna falta.
    // El llamador (ParsearArchivo en ConsolidacionService) atrapa la excepción, la registra
    // como "Fallido" y continúa con los demás archivos sin detener la consolidación.
    internal static void ValidarColumnas(
        IEnumerable<string> presentes,
        string[]            requeridas,
        string              nombreArchivo)
    {
        var presentesSet = new HashSet<string>(presentes, StringComparer.OrdinalIgnoreCase);
        var faltantes    = requeridas.Where(r => !presentesSet.Contains(r)).ToList();
        if (faltantes.Count > 0)
            throw new InvalidOperationException(
                $"{nombreArchivo}: columnas requeridas no encontradas — {string.Join(", ", faltantes)}");
    }
}
