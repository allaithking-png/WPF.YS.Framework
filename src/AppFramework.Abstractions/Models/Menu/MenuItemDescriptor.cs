using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Menu;

/// <summary>
/// وصف عنصر قائمة واحد.
/// </summary>
public sealed class MenuItemDescriptor
{
    /// <summary>معرّف فريد.</summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString();

    /// <summary>النص المعروض.</summary>
    public string Title { get; set; } = "";

    /// <summary>اسم الأيقونة.</summary>
    public string? Icon { get; set; }

    /// <summary>مفتاح الأمر (يُحلّ عبر IMenuManager).</summary>
    public string? CommandKey { get; set; }

    ///// <summary>الأمر المباشر (بديل عن CommandKey).</summary>
    //public System.Windows.Input.ICommand? Command { get; set; }

    /// <summary>بارامتر الأمر.</summary>
    public object? CommandParameter { get; set; }

    /// <summary>عناصر فرعية.</summary>
    public List<MenuItemDescriptor> Children { get; set; } = new();

    /// <summary>هل هذا فاصل (Separator)؟</summary>
    public bool IsSeparator { get; set; }

    /// <summary>ترتيب العرض.</summary>
    public int Order { get; set; }

    /// <summary>اسم المجموعة (للريبون: RibbonGroup).</summary>
    public string? GroupName { get; set; }

    /// <summary>هل العنصر مفعّل؟</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>هل العنصر مرئي؟</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>هل العنصر قابل للتبديل (Toggle)؟</summary>
    public bool IsToggle { get; set; }

    /// <summary>هل العنصر مُبدَّل حاليًا (فقط لو IsToggle)؟</summary>
    public bool IsChecked { get; set; }
}