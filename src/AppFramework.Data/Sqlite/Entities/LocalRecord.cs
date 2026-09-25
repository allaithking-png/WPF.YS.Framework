using System;

namespace AppFramework.Data.Sqlite.Entities;

/// <summary>
/// سجل محلي لكيان (صف) من أي مصدر بيانات.
/// يخزّن الكيان كـ JSON للعمل مع أي نوع دون schema مخصص.
/// </summary>
public sealed class LocalRecord
{
    /// <summary>معرّف داخلي.</summary>
    public Guid Id { get; set; }

    /// <summary>مفتاح مصدر البيانات (مثال: "Orders").</summary>
    public string SourceKey { get; set; } = "";

    /// <summary>مفتاح العنصر داخليًا.</summary>
    public string ItemKey { get; set; } = "";

    /// <summary>اسم النوع (AssemblyQualifiedName).</summary>
    public string ClrType { get; set; } = "";

    /// <summary>الكيان مُسلسَلًا (JSON).</summary>
    public string Payload { get; set; } = "";

    /// <summary>آخر تحديث (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}