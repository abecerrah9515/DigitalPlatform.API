using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DigitalPlatform.Infrastructure.Parsers;

internal static class ExcelParserHelper
{
    internal static Dictionary<string, int> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var key = Normalize(cell.GetString());
            if (!string.IsNullOrWhiteSpace(key))
                map.TryAdd(key, cell.Address.ColumnNumber);
        }
        return map;
    }

    internal static string Normalize(string s)
    {
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return Regex.Replace(sb.ToString().ToLowerInvariant(), @"\s+", " ").Trim();
    }

    internal static string GetString(IXLRow row, int col) =>
        col > 0 ? row.Cell(col).GetString().Trim() : string.Empty;

    internal static int GetInt(IXLRow row, int col)
    {
        if (col <= 0) return 0;
        var cell = row.Cell(col);
        if (cell.TryGetValue<int>(out var v)) return v;
        return int.TryParse(cell.GetString(), out var p) ? p : 0;
    }

    internal static decimal GetDecimal(IXLRow row, int col)
    {
        if (col <= 0) return 0m;
        var cell = row.Cell(col);
        if (cell.TryGetValue<decimal>(out var v)) return v;
        return decimal.TryParse(cell.GetString(),
            NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0m;
    }

    internal static DateOnly GetDateOnly(IXLRow row, int col)
    {
        if (col <= 0) return DateOnly.MinValue;
        var cell = row.Cell(col);
        if (cell.TryGetValue<DateTime>(out var dt)) return DateOnly.FromDateTime(dt);
        return DateOnly.TryParse(cell.GetString(), out var d) ? d : DateOnly.MinValue;
    }
}
