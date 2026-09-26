using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Reports;

namespace AppFramework.Data.Reports;

/// <summary>
/// مسؤول عن حفظ تقرير إلى ملف على القرص.
/// </summary>
public interface IReportExporter
{
    ReportExportFormat Format { get; }

    /// <summary>حفظ النتيجة إلى مسار.</summary>
    Task ExportAsync(ReportResult result, string targetPath, CancellationToken ct = default);
}