using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Core.Screens;

/// <summary>
/// خيارات لعرض شاشات القوائم العامة.
/// </summary>
public sealed class ListScreenOptions
{
    /// <summary>الأعمدة (اسم الخاصية + العنوان).</summary>
    public List<ColumnDefinition> Columns { get; set; } = new();

    /// <summary>الوضع الافتراضي.</summary>
    public ViewMode DefaultMode { get; set; } = ViewMode.Grid;

    /// <summary>هل يمكن الإضافة؟</summary>
    public bool CanAdd { get; set; } = true;

    /// <summary>هل يمكن التعديل؟</summary>
    public bool CanEdit { get; set; } = true;

    /// <summary>هل يمكن الحذف؟</summary>
    public bool CanDelete { get; set; } = true;

    /// <summary>حقول البحث.</summary>
    public List<string> SearchFields { get; set; } = new();
}

/// <summary>تعريف عمود.</summary>
public sealed class ColumnDefinition
{
    public string PropertyName { get; set; } = "";
    public string Header { get; set; } = "";
    public double Width { get; set; } = 120;
    public bool IsPrimary { get; set; }
    public string? Format { get; set; }
}