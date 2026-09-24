namespace AppFramework.Abstractions.Models.Reports;

/// <summary>
/// نتيجة توليد تقرير.
/// </summary>
public sealed class ReportResult
{
    /// <summary>معرّف التقرير.</summary>
    public string ReportId { get; set; } = "";

    /// <summary>بيانات التقرير (DataTable / IEnumerable / ...).</summary>
    public object? Data { get; set; }

    /// <summary>البايتات المُصيَّرة (إن كان PDF/Excel جاهزًا).</summary>
    public byte[]? RenderedBytes { get; set; }

    /// <summary>مسار الملف (إن حُفظ على القرص).</summary>
    public string? FilePath { get; set; }
}