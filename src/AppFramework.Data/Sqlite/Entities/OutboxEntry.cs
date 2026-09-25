using System;
using AppFramework.Abstractions.Models.Data;

namespace AppFramework.Data.Sqlite.Entities;

/// <summary>
/// مدخل Outbox — تغيير محلي في انتظار الإرسال للسيرفر.
/// </summary>
public sealed class OutboxEntry
{
    /// <summary>معرّف داخلي.</summary>
    public Guid Id { get; set; }

    /// <summary>مفتاح مصدر البيانات.</summary>
    public string SourceKey { get; set; } = "";

    /// <summary>مفتاح العنصر (إن وُجد).</summary>
    public string? ItemKey { get; set; }

    /// <summary>نوع التغيير (Added/Updated/Deleted/Refreshed).</summary>
    public DataChangeKind Kind { get; set; }

    /// <summary>الحمولة (JSON).</summary>
    public string? Payload { get; set; }

    /// <summary>اسم نوع الحمولة (AssemblyQualifiedName).</summary>
    public string? ClrType { get; set; }

    /// <summary>وقت الإنشاء (UTC).</summary>
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>عدد محاولات الإرسال.</summary>
    public int Attempts { get; set; }

    /// <summary>آخر خطأ (إن وُجد).</summary>
    public string? LastError { get; set; }
}