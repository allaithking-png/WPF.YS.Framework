using System;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Reports;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// خدمة التقارير.
/// </summary>
public interface IReportService
{
    /// <summary>تسجيل تقرير بدالّة توليد.</summary>
    void RegisterReport(string reportId, Func<ReportRequest, Task<ReportResult>> generator);

    /// <summary>توليد تقرير.</summary>
    Task<ReportResult> GenerateAsync(ReportRequest request, CancellationToken ct = default);

    /// <summary>تصيير التقرير بصيغة معيّنة.</summary>
    Task<byte[]> RenderAsync(ReportRequest request, ReportExportFormat format, CancellationToken ct = default);

    /// <summary>عرض معاينة التقرير في نافذة.</summary>
    void ShowPreview(ReportResult result);
}