using System;
using System.Collections.Generic;
using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة الشاشة على دعم أكثر من طريقة عرض (Grid, Card, Tree, Timeline, ...).
/// </summary>
public interface IViewModeAware : IAppAware
{
    /// <summary>طرق العرض المدعومة.</summary>
    IReadOnlyList<ViewMode> SupportedModes { get; }

    /// <summary>طريقة العرض الحالية.</summary>
    ViewMode CurrentMode { get; }

    /// <summary>حدث يُطلَق عند تبديل طريقة العرض.</summary>
    event EventHandler<ViewMode>? ModeChanged;

    /// <summary>تبديل طريقة العرض.</summary>
    void SetMode(ViewMode mode);
}
