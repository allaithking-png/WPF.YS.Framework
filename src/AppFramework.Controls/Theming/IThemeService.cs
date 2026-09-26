using System;
using System.Collections.Generic;

namespace AppFramework.Controls.Theming;

/// <summary>
/// خدمة إدارة الثيمات (Light/Dark/Corporate).
/// </summary>
public interface IThemeService
{
    /// <summary>الثيم الحالي.</summary>
    AppTheme CurrentTheme { get; }

    /// <summary>قائمة الثيمات المتاحة.</summary>
    IReadOnlyCollection<AppTheme> AvailableThemes { get; }

    /// <summary>حدث يُطلَق عند تغيير الثيم.</summary>
    event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>تطبيق ثيم.</summary>
    void ApplyTheme(AppTheme theme);

    /// <summary>تحميل آخر ثيم محفوظ للمستخدم (إن وُجد).</summary>
    void ApplySavedTheme(string? userId = null);

    /// <summary>حفظ الثيم الحالي للمستخدم.</summary>
    void SaveCurrentTheme(string? userId = null);
}

/// <summary>الثيمات المتاحة.</summary>
public enum AppTheme
{
    Light,
    Dark,
    Corporate
}