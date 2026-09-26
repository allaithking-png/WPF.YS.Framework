using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Reports;

namespace AppFramework.Data.Reports;

/// <summary>
/// مسؤول عن تصيير تقرير إلى بايتات (PDF/Excel/...).
/// </summary>
public interface IReportRenderer
{
    /// <summary>الصيغة التي يدعمها هذا الـ Renderer.</summary>
    ReportExportFormat Format { get; }

    /// <summary>تصيير النتيجة إلى bytes.</summary>
    Task<byte[]> RenderAsync(ReportResult result, CancellationToken ct = default);
}