using System.Globalization;
using System.Text;

namespace MDMS.Api.Reporting;

/// <summary>Minimal CSV writer — no external dependency needed for the column counts/row volumes
/// these reports deal with. Quotes every field and escapes embedded quotes per RFC 4180.</summary>
public static class CsvWriter
{
    public static byte[] Write<T>(IReadOnlyList<string> headers, IEnumerable<T> rows, Func<T, IReadOnlyList<object?>> selectRow)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
        {
            var values = selectRow(row).Select(FormatValue);
            sb.AppendLine(string.Join(",", values.Select(Escape)));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "",
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        decimal d => d.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString(CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static string Escape(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }
}
