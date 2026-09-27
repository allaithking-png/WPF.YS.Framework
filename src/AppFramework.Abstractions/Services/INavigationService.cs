using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// خدمة التنقل بين الشاشات.
/// </summary>
public interface INavigationService
{
    /// <summary>الشاشة الحالية.</summary>
    NavigationContext? Current { get; }

    /// <summary>فتح شاشة.</summary>
    Task<NavigationResult> OpenScreenAsync(string screenId, NavigationContext? context = null);

    /// <summary>إغلاق شاشة.</summary>
    Task CloseScreenAsync(string screenId);

    /// <summary>فتح تقرير.</summary>
    Task<NavigationResult> OpenReportAsync(string reportId, IReadOnlyDictionary<string, object?>? parameters = null);

    /// <summary>فتح رابط خارجي.</summary>
    Task OpenUrlAsync(string url);

    /// <summary>حدث عند بدء التنقل.</summary>
    event EventHandler<NavigationContext>? Navigating;

    /// <summary>حدث بعد إتمام التنقل.</summary>
    event EventHandler<NavigationContext>? Navigated;
    // ==========================================================
    //  Open Screens Management
    // ==========================================================

    /// <summary>كل الشاشات المفتوحة حاليًا.</summary>
    IReadOnlyList<OpenScreenInfo> OpenScreens { get; }

    /// <summary>الشاشة النشطة حاليًا (معرّفها).</summary>
    string? ActiveScreenId { get; }

    /// <summary>حدث عند فتح شاشة.</summary>
    event EventHandler<OpenScreenInfo>? ScreenOpened;

    /// <summary>حدث عند إغلاق شاشة.</summary>
    event EventHandler<OpenScreenInfo>? ScreenClosed;

    /// <summary>حدث عند تغيير الشاشة النشطة.</summary>
    event EventHandler<string?>? ActiveScreenChanged;

    /// <summary>يُفعّل شاشة مفتوحة (يُبدّل إليها).</summary>
    Task<bool> ActivateScreenAsync(string screenId);

    /// <summary>يُغلق كل الشاشات عدا المذكورة.</summary>
    Task CloseAllExceptAsync(string screenId);

    /// <summary>يُغلق كل الشاشات.</summary>
    Task CloseAllAsync();
}
/// <summary>معلومات عن شاشة مفتوحة (للعرض في Taskbar).</summary>
public sealed record OpenScreenInfo(
    string ScreenId,
    string InstanceKey,
    string Title,
    string? Icon,
    bool IsActive,
    DateTime OpenedAt);
