using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AppFramework.Abstractions.Models.Reports;

namespace AppFramework.Data.Reports.Exporters;

/// <summary>
/// مصدّر PDF عبر QuestPDF.
/// </summary>
/// <remarks>
/// يتطلب: QuestPDF.Settings.License = LicenseType.Community;
/// </remarks>
public sealed class PdfReportExporter : IReportExporter, IReportRenderer
{
    public ReportExportFormat Format => ReportExportFormat.Pdf;

    public Task<byte[]> RenderAsync(ReportResult result, CancellationToken ct = default)
    {
        var bytes = BuildPdf(result);
        return Task.FromResult(bytes);
    }

    public async Task ExportAsync(ReportResult result, string targetPath, CancellationToken ct = default)
    {
        // إن كانت البايتات جاهزة، اكتبها مباشرة
        if (result.RenderedBytes is { Length: > 0 })
        {
            await File.WriteAllBytesAsync(targetPath, result.RenderedBytes, ct);
            return;
        }

        var bytes = BuildPdf(result);
        await File.WriteAllBytesAsync(targetPath, bytes, ct);
    }

    // ==========================================================
    //  PDF Builder
    // ==========================================================

    private static byte[] BuildPdf(ReportResult result)
    {
        var items = ExtractItems(result.Data);
        var title = string.IsNullOrEmpty(result.ReportId) ? "تقرير" : result.ReportId;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(header =>
                {
                    header.PaddingBottom(10).Text(title)
                          .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                });

                page.Content().Element(content =>
                {
                    if (items.Count == 0)
                    {
                        content.Text("(لا توجد بيانات)").Italic();
                        return;
                    }

                    var props = items[0].GetType().GetProperties(
                        BindingFlags.Public | BindingFlags.Instance);

                    content.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in props)
                                columns.RelativeColumn();
                        });

                        // Header row
                        table.Header(h =>
                        {
                            foreach (var prop in props)
                            {
                                h.Cell().Background(Colors.Grey.Lighten2)
                                 .Padding(4)
                                 .Text(prop.Name).Bold();
                            }
                        });

                        // Data rows
                        foreach (var item in items)
                        {
                            foreach (var prop in props)
                            {
                                var val = prop.GetValue(item)?.ToString() ?? "";
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                     .Padding(4).Text(val);
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("الصفحة ").FontSize(8);
                    txt.CurrentPageNumber().FontSize(8);
                    txt.Span(" من ").FontSize(8);
                    txt.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
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
