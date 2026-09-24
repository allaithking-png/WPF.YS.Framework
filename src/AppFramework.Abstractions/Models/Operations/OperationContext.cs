using System;
using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Operations;

/// <summary>
/// سياق عملية قيد التنفيذ (حفظ، حذف، تصدير، ...).
/// يُمرَّر إلى IOperationAware.OnOperationStarting قبل التنفيذ.
/// </summary>
public sealed class OperationContext
{
    /// <summary>معرّف فريد للعملية.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>نوع العملية (Save, Delete, Export, ...).</summary>
    public string Type { get; init; } = "";

    /// <summary>معرّف العنصر الهدف (إن وُجد).</summary>
    public string? TargetId { get; init; }

    /// <summary>بارامترات العملية.</summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();

    /// <summary>هل ألغى أحد المشتركين العملية؟</summary>
    public bool Cancel { get; set; }
}