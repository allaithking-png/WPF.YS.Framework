namespace AppFramework.Abstractions.Models.Screens;

/// <summary>
/// نمط عرض الشاشة (وليس طريقة عرض البيانات).
/// </summary>
public enum ScreenViewMode
{
    /// <summary>قراءة فقط.</summary>
    ReadOnly,

    /// <summary>قراءة وكتابة.</summary>
    ReadWrite,

    /// <summary>عرض المحتوى فقط (إخفاء أدوات التحرير).</summary>
    ContentOnly,

    /// <summary>عرض سردي (List).</summary>
    Narrative,

    /// <summary>عرض زمني (Timeline).</summary>
    Timeline
}