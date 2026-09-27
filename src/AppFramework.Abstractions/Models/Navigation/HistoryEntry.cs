using System;

namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// سطر واحد في سجل التغييرات.
/// </summary>
public sealed class HistoryEntry
{
    /// <summary>معرّف فريد.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>معرّف العنصر المتأثر (قد يكون null للأحداث العامة).</summary>
    public string? ItemId { get; set; }

    /// <summary>نوع الحدث.</summary>
    public HistoryKind Kind { get; set; }

    /// <summary>عنوان مختصر.</summary>
    public string Title { get; set; } = "";

    /// <summary>تفاصيل إضافية.</summary>
    public string? Details { get; set; }

    /// <summary>معرّف المستخدم الذي أنشأ الحدث.</summary>
    public string? UserId { get; set; }

    /// <summary>وقت الحدث (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// مفاتيح المستخدمين الذين شاهدوا الحدث.
    /// (لكل مستخدم IsSeen خاص به).
    /// </summary>
    public System.Collections.Generic.HashSet<string> SeenBy { get; set; } = new();

    // ==========================================================
    //  Helpers
    // ==========================================================

    /// <summary>هل شاهده هذا المستخدم؟</summary>
    public bool IsSeenBy(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        return SeenBy.Contains(userId);
    }

    /// <summary>علّم كمشاهَد من مستخدم.</summary>
    public void MarkSeenBy(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;
        SeenBy.Add(userId);
    }

    /// <summary>الوقت المنقضي (وصف بشري).</summary>
    public string GetRelativeTime()
    {
        var elapsed = DateTime.UtcNow - Timestamp;

        if (elapsed.TotalMinutes < 1) return "الآن";
        if (elapsed.TotalMinutes < 60) return $"قبل {(int)elapsed.TotalMinutes} دقيقة";
        if (elapsed.TotalHours < 24) return $"قبل {(int)elapsed.TotalHours} ساعة";
        if (elapsed.TotalDays < 7) return $"قبل {(int)elapsed.TotalDays} يوم";
        if (elapsed.TotalDays < 30) return $"قبل {(int)(elapsed.TotalDays / 7)} أسبوع";
        if (elapsed.TotalDays < 365) return $"قبل {(int)(elapsed.TotalDays / 30)} شهر";
        return $"قبل {(int)(elapsed.TotalDays / 365)} سنة";
    }

    /// <summary>أيقونة حسب النوع.</summary>
    public string GetIcon() => Kind switch
    {
        HistoryKind.Created => "✨",
        HistoryKind.Updated => "🔄",
        HistoryKind.Deleted => "🗑️",
        HistoryKind.Moved => "↔️",
        HistoryKind.AddedToFolder => "📁",
        HistoryKind.RemovedFromFolder => "📂",
        HistoryKind.SettingsChanged => "⚙️",
        _ => "ℹ️"
    };

    /// <summary>وصف نوع الحدث.</summary>
    public string GetKindLabel() => Kind switch
    {
        HistoryKind.Created => "جديد",
        HistoryKind.Updated => "محدّث",
        HistoryKind.Deleted => "محذوف",
        HistoryKind.Moved => "منقول",
        HistoryKind.AddedToFolder => "أُضيف لمجلد",
        HistoryKind.RemovedFromFolder => "أُزيل من مجلد",
        HistoryKind.SettingsChanged => "تغيير إعدادات",
        _ => "حدث"
    };
}