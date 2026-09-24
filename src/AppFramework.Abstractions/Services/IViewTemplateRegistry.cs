using System;
using System.Collections.Generic;
using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// سجل قوالب العرض لكل ViewModel.
/// </summary>
public interface IViewTemplateRegistry
{
    /// <summary>تسجيل ViewModel مع عدة قوالب.</summary>
    void Register(Type viewModelType, params ViewTemplateDescriptor[] templates);

    /// <summary>إضافة قالب واحد لـ ViewModel موجود.</summary>
    void RegisterTemplate(Type viewModelType, ViewTemplateDescriptor template);

    /// <summary>إزالة قالب.</summary>
    void Unregister(Type viewModelType, ViewMode mode);

    /// <summary>الحصول على كل القوالب لـ ViewModel.</summary>
    IReadOnlyList<ViewTemplateDescriptor> GetTemplates(Type viewModelType);

    /// <summary>الحصول على كل القوالب لـ ViewModel.</summary>
    IReadOnlyList<ViewTemplateDescriptor> GetTemplates<TViewModel>();

    /// <summary>الحصول على القالب الافتراضي.</summary>
    ViewTemplateDescriptor? GetDefault(Type viewModelType);

    /// <summary>الحصول على قالب بطريقة عرض محددة.</summary>
    ViewTemplateDescriptor? GetByMode(Type viewModelType, ViewMode mode);

    /// <summary>حدث عند تغيير قوالب ViewModel.</summary>
    event EventHandler<Type>? TemplatesChanged;
}