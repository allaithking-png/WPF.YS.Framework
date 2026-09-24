using System;
using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.ViewTemplates;

/// <summary>
/// وصف قالب عرض واحد (طريقة عرض + View مصاحب).
/// </summary>
public sealed class ViewTemplateDescriptor
{
    /// <summary>معرّف فريد.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>طريقة العرض.</summary>
    public ViewMode Mode { get; init; }

    /// <summary>التصنيف (أساسية/إضافية).</summary>
    public ViewModeCategory Category { get; init; } = ViewModeCategory.Extended;

    /// <summary>العنوان المعروض للمستخدم.</summary>
    public string Title { get; init; } = "";

    /// <summary>اسم الأيقونة.</summary>
    public string? Icon { get; init; }

    /// <summary>وصف مختصر.</summary>
    public string? Description { get; init; }

    /// <summary>نوع الـ View (UserControl أو FrameworkElement).</summary>
    public Type? ViewType { get; init; }

    /// <summary>نوع الـ ViewModel الخاص بطريقة العرض (اختياري).</summary>
    public Type? ViewModelType { get; init; }

    /// <summary>هل هذه طريقة العرض الافتراضية للـ ViewModel؟</summary>
    public bool IsDefault { get; init; }

    /// <summary>بيانات وصفية إضافية.</summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}