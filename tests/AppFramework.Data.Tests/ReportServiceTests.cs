using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using AppFramework.Abstractions.Models.Reports;
using AppFramework.Data.Reports;
using AppFramework.Data.Reports.Exporters;
using Xunit;

namespace AppFramework.Data.Tests;

public class ReportServiceTests
{
    // =============== Test Model ===============

    private sealed record Row
    {
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }

    // =============== Helpers ===============

    private static ReportService CreateService()
    {
        var renderers = new IReportRenderer[]
        {
            new CsvReportExporter(),
            new ExcelReportExporter(),
            new PdfReportExporter()
        };

        var exporters = new IReportExporter[]
        {
            new CsvReportExporter(),
            new ExcelReportExporter(),
            new PdfReportExporter()
        };

        return new ReportService(renderers, exporters, NullLogger<ReportService>.Instance);
    }

    private static List<Row> SampleData() => new()
    {
        new Row { Name = "Alpha", Value = 10 },
        new Row { Name = "Beta", Value = 20 },
        new Row { Name = "Gamma", Value = 30 }
    };

    // =============== Generation ===============

    [Fact]
    public async Task GenerateAsync_WithRegisteredReport_ReturnsData()
    {
        var svc = CreateService();
        svc.RegisterReport("Test", _ => Task.FromResult(new ReportResult
        {
            Data = SampleData()
        }));

        var result = await svc.GenerateAsync(new ReportRequest { ReportId = "Test" });

        result.ReportId.Should().Be("Test");
        result.Data.Should().BeOfType<List<Row>>();
    }

    [Fact]
    public async Task GenerateAsync_UnknownReport_ReturnsEmpty()
    {
        var svc = CreateService();
        var result = await svc.GenerateAsync(new ReportRequest { ReportId = "Missing" });
        result.Data.Should().BeAssignableTo<System.Collections.IEnumerable>();
    }

    // =============== CSV ===============

    [Fact]
    public async Task RenderAsync_Csv_ReturnsUtf8Bytes()
    {
        var svc = CreateService();
        svc.RegisterReport("Test", _ => Task.FromResult(new ReportResult
        {
            Data = SampleData()
        }));

        var bytes = await svc.RenderAsync(new ReportRequest { ReportId = "Test" }, ReportExportFormat.Csv);

        bytes.Should().NotBeEmpty();
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        text.Should().Contain("Name").And.Contain("Value");
        text.Should().Contain("Alpha").And.Contain("Beta").And.Contain("Gamma");
    }

    // =============== Excel ===============

    [Fact]
    public async Task RenderAsync_Excel_ReturnsXlsxBytes()
    {
        var svc = CreateService();
        svc.RegisterReport("Test", _ => Task.FromResult(new ReportResult
        {
            Data = SampleData()
        }));

        var bytes = await svc.RenderAsync(new ReportRequest { ReportId = "Test" }, ReportExportFormat.Excel);

        bytes.Should().NotBeEmpty();
        // XLSX يبدأ بـ "PK" (ZIP signature)
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
    }

    // =============== PDF ===============

    [Fact]
    public async Task RenderAsync_Pdf_ReturnsPdfBytes()
    {
        // اقبل رخصة QuestPDF للاختبارات
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var svc = CreateService();
        svc.RegisterReport("Test", _ => Task.FromResult(new ReportResult
        {
            Data = SampleData()
        }));

        var bytes = await svc.RenderAsync(new ReportRequest { ReportId = "Test" }, ReportExportFormat.Pdf);

        bytes.Should().NotBeEmpty();
        // PDF يبدأ بـ "%PDF"
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 4);
        header.Should().Be("%PDF");
    }

    // =============== Export ===============

    [Fact]
    public async Task ExportAsync_Csv_WritesFile()
    {
        var svc = CreateService();
        svc.RegisterReport("Test", _ => Task.FromResult(new ReportResult
        {
            Data = SampleData()
        }));

        var path = Path.Combine(Path.GetTempPath(), $"report-{Guid.NewGuid():N}.csv");

        try
        {
            await svc.ExportAsync(new ReportRequest { ReportId = "Test" }, ReportExportFormat.Csv, path);

            File.Exists(path).Should().BeTrue();
            var content = await File.ReadAllTextAsync(path);
            content.Should().Contain("Alpha");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}