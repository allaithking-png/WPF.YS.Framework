using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Menu;

/// <summary>
/// سياق بناء القائمة (يُمرَّر إلى IMenuAware.BuildMenu).
/// </summary>
public sealed class MenuContext
{
    /// <summary>معرّف الشاشة.</summary>
    public string ScreenId { get; set; } = "";

    /// <summary>الـ ViewModel المرتبط بالشاشة.</summary>
    public object? ViewModel { get; set; }

    /// <summary>الـ View المرتبط بالشاشة.</summary>
    public object? View { get; set; }

    /// <summary>حالة سياقية إضافية.</summary>
    public Dictionary<string, object> State { get; } = new();

    /// <summary>مكان العرض المفضل.</summary>
    public MenuHost PreferredHost { get; set; } = MenuHost.MenuBar;
}