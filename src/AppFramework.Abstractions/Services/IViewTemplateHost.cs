using AppFramework.Abstractions.Models.ViewTemplates;
using System;
using System.Windows;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// خدمة عرض ViewModel بطريقة عرض محددة داخل Region.
/// </summary>
public interface IViewTemplateHost
{
    /// <summary>طريقة العرض الحالية.</summary>
    ViewMode CurrentMode { get; }
    /// <summary>العرض الحالي (آخر FrameworkElement مُنتَج).</summary>
    FrameworkElement? CurrentView { get; }

    /// <summary>عرض ViewModel بطريقة عرض محددة في Region.</summary>
    void ShowInMode(object viewModel, ViewMode mode, string regionName);

    /// <summary>تبديل طريقة العرض الحالية.</summary>
    void SwitchMode(ViewMode mode);

    /// <summary>حدث عند تغيير طريقة العرض.</summary>
    event EventHandler<ViewMode>? ModeChanged;
    /// <summary>حدث يُطلَق عند توليد View جديد.</summary>
    event EventHandler<ViewRenderedEventArgs>? ViewRendered;
}
/// <summary>حدث ViewRendered.</summary>
public sealed class ViewRenderedEventArgs : EventArgs
{
    public FrameworkElement View { get; }
    public ViewMode Mode { get; }

    public ViewRenderedEventArgs(FrameworkElement view, ViewMode mode)
    {
        View = view;
        Mode = mode;
    }
}
