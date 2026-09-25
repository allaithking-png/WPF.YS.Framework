using System;
using System.Collections.Generic;

namespace AppFramework.Core.Navigation;

/// <summary>
/// جلسة تشغيل — تُستخدم لحفظ/استرجاع حالة الشاشات المفتوحة.
/// </summary>
public sealed class NavigationSession
{
    /// <summary>معرّف المستخدم (للفصل بين الجلسات).</summary>
    public string UserId { get; set; } = "";

    /// <summary>وقت الحفظ (UTC).</summary>
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>الشاشات المفتوحة.</summary>
    public List<OpenScreenState> OpenScreens { get; set; } = new();

    /// <summary>معرّف الشاشة النشطة (التي كانت في المقدمة).</summary>
    public string? ActiveScreenId { get; set; }
}

/// <summary>حالة شاشة مفتوحة واحدة.</summary>
public sealed class OpenScreenState
{
    /// <summary>معرّف الشاشة.</summary>
    public string ScreenId { get; set; } = "";

    /// <summary>معرّف النسخة (للـ Multi-Open).</summary>
    public string? InstanceId { get; set; }

    /// <summary>الموقع المستهدف داخل الشاشة (سطر معيّن، تبويب فرعي...).</summary>
    public string? TargetLocation { get; set; }

    /// <summary>بارامترات التنقل.</summary>
    public Dictionary<string, object?>? Parameters { get; set; }

    /// <summary>ترتيب التبويب (للـ Multi-Open).</summary>
    public int Order { get; set; }
}