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
}