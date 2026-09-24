using System;
using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// خدمة عرض ViewModel بطريقة عرض محددة داخل Region.
/// </summary>
public interface IViewTemplateHost
{
    /// <summary>طريقة العرض الحالية.</summary>
    ViewMode CurrentMode { get; }

    /// <summary>عرض ViewModel بطريقة عرض محددة في Region.</summary>
    void ShowInMode(object viewModel, ViewMode mode, string regionName);

    /// <summary>تبديل طريقة العرض الحالية.</summary>
    void SwitchMode(ViewMode mode);

    /// <summary>حدث عند تغيير طريقة العرض.</summary>
    event EventHandler<ViewMode>? ModeChanged;
}