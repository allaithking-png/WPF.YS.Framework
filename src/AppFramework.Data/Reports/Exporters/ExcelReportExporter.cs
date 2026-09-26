using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using AppFramework.Abstractions.Models.Reports;

namespace AppFramework.Data.Reports.Exporters;

/// <summary>
/// مصدّر Excel (xlsx) عبر ClosedXML.
/// </summary>
public sealed class ExcelReportExporter : IReportExporter, IReportRenderer
{
    public ReportExportFormat Format => ReportExportFormat.Excel;

    public Task<byte[]> RenderAsync(ReportResult result, CancellationToken ct = default)
    {
        using var workbook = BuildWorkbook(result);
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public async Task ExportAsync(ReportResult result, string targetPath, CancellationToken ct = default)
    {
        // إن كانت البايتات جاهزة، اكتبها مباشرة
        if (result.RenderedBytes is { Length: > 0 })
        {
            await File.WriteAllBytesAsync(targetPath, result.RenderedBytes, ct);
            return;
        }

        await Task.Run(() =>
        {
            using var workbook = BuildWorkbook(result);
            workbook.SaveAs(targetPath);
        }, ct);
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private static XLWorkbook BuildWorkbook(ReportResult result)
    {
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(string.IsNullOrEmpty(result.ReportId) ? "Report" : result.ReportId);

        var items = ExtractItems(result.Data);
        if (items.Count == 0)
        {
            ws.Cell(1, 1).Value = "(لا توجد بيانات)";
            return wb;
        }

        var firstType = items[0].GetType();
        var props = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Header
        for (int c = 0; c < props.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = props[c].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Rows
        for (int r = 0; r < items.Count; r++)
        {
            var item = items[r];
            for (int c = 0; c < props.Length; c++)
            {
                var val = props[c].GetValue(item);
                var cell = ws.Cell(r + 2, c + 1);

                switch (val)
                {
                    case null:
                        cell.Value = "";
                        break;
                    case DateTime dt:
                        cell.Value = dt;
                        break;
                    case bool b:
                        cell.Value = b;
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case long l:
                        cell.Value = l;
                        break;
                    case decimal d:
                        cell.Value = d;
                        break;
                    case double db:
                        cell.Value = db;
                        break;
                    default:
                        cell.Value = val.ToString();
                        break;
                }
            }
        }

        ws.Columns().AdjustToContents();
        return wb;
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
