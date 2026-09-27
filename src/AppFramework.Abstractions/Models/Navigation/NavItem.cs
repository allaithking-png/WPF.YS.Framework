using System;
using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// عنصر في مستكشف التنقل — يمكن أن يكون شاشة، تقريرًا، مجلدًا، ملفًا، رابطًا.
/// </summary>
public sealed class NavItem
{
    /// <summary>معرّف فريد.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>العنوان المعروض.</summary>
    public string Title { get; set; } = "";

    /// <summary>اسم الأيقونة.</summary>
    public string? Icon { get; set; }

    /// <summary>نوع العنصر.</summary>
    public NavItemKind Kind { get; set; } = NavItemKind.Screen;

    /// <summary>
    /// الهدف (ScreenId / ReportId / Path / Url).
    /// يُستخدم عند فتح العنصر.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>بارامترات التنقل (للتوجّه لموقع محدد داخل الشاشة).</summary>
    public Dictionary<string, object?>? Parameters { get; set; }

    /// <summary>عناصر فرعية (للمجلدات).</summary>
    public List<NavItem> Children { get; set; } = new();

    /// <summary>هل العنصر في المفضلة؟</summary>
    public bool IsFavorite { get; set; }

    /// <summary>هل العنصر مثبّت؟</summary>
    public bool IsPinned { get; set; }

    /// <summary>ترتيب العرض.</summary>
    public int SortOrder { get; set; }

    /// <summary>عدد مرات الاستخدام (يُحدَّث من الخدمة).</summary>
    public int UsageCount { get; set; }

    /// <summary>آخر مرة فُتح (UTC).</summary>
    public DateTime? LastOpened { get; set; }

    /// <summary>أول مرة عُرف (UTC).</summary>
    public DateTime? FirstSeen { get; set; }

    /// <summary>مسار التصنيف (مثال: "Sales/Orders").</summary>
    public string? CategoryPath { get; set; }

    /// <summary>وصف مختصر.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// بيانات وصفية إضافية (لاستخدامات مخصصة).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>هل العنصر يحتوي على أبناء؟</summary>
    public bool HasChildren => Children.Count > 0;


    /// <summary>هل العنصر جديد (لم يُفتح بعد)؟</summary>
    public bool IsNew { get; set; }

    /// <summary>هل العنصر محدّث مؤخرًا؟</summary>
    public bool IsUpdated { get; set; }

    /// <summary>آخر تحديث (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>مجلد أب (لتجميع العناصر اليدوية).</summary>
    public string? ParentFolderId { get; set; }

    /// <summary>هل هذا العنصر هو الشاشة النشطة؟</summary>
    public bool IsActive { get; set; }
    ///// <summary>ترتيب العرض في WrapPanel (الأصغر أولًا).</summary>
    //public int Order { get; set; }

    /// <summary>حجم مخصص للأيقونة (إن أردت حجمًا مختلفًا عن الافتراضي).</summary>
    public double? CustomIconSize { get; set; }

    /// <summary>لون خلفية مخصص للأيقونة (HEX).</summary>
    public string? CustomBackground { get; set; }
}
