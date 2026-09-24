using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Reports;

/// <summary>
/// طلب توليد تقرير.
/// </summary>
public sealed class ReportRequest
{
    /// <summary>معرّف التقرير.</summary>
    public string ReportId { get; set; } = "";

    /// <summary>بارامترات التقرير.</summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>عنوان مخصص (اختياري).</summary>
    public string? Title { get; set; }
}