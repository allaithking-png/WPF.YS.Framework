using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Reports;

namespace AppFramework.Data.Reports.Exporters;

/// <summary>
/// مصدّر CSV بسيط.
/// </summary>
public sealed class CsvReportExporter : IReportExporter, IReportRenderer
{
    public ReportExportFormat Format => ReportExportFormat.Csv;

    public Task<byte[]> RenderAsync(ReportResult result, CancellationToken ct = default)
    {
        var csv = BuildCsv(result);
        return Task.FromResult(Encoding.UTF8.GetBytes(csv));
    }

    public async Task ExportAsync(ReportResult result, string targetPath, CancellationToken ct = default)
    {
        // إن كانت البايتات جاهزة، اكتبها مباشرة
        if (result.RenderedBytes is { Length: > 0 })
        {
            await File.WriteAllBytesAsync(targetPath, result.RenderedBytes, ct);
            return;
        }

        // وإلا ابنِ CSV من Data
        var csv = BuildCsv(result);
        await File.WriteAllTextAsync(targetPath, csv, Encoding.UTF8, ct);
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private static string BuildCsv(ReportResult result)
    {
        var items = ExtractItems(result.Data);
        if (items.Count == 0) return "";

        var sb = new StringBuilder();

        // Header
        var firstType = items[0].GetType();
        var props = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        sb.AppendLine(string.Join(",", props.Select(p => Escape(p.Name))));

        // Rows
        foreach (var item in items)
        {
            var values = props.Select(p =>
            {
                var val = p.GetValue(item);
                return Escape(val?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static List<object> ExtractItems(object? data)
    {
        if (data is null) return new List<object>();
        if (data is IEnumerable enumerable && data is not string)
        {
            var result = new List<object>();
            foreach (var item in enumerable)
                if (item is not null) result.Add(item);
            return result;
        }
        return new List<object> { data };
    }
}
