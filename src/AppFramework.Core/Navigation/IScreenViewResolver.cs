using System;

namespace AppFramework.Core.Navigation;

/// <summary>
/// مسؤول عن إيجاد/إنشاء الـ View المناسب لـ ViewModel معيّن.
/// </summary>
/// <remarks>
/// هذا الفصل يسمح بتغيير استراتيجية ربط View/ViewModel
/// (Convention-based, DataTemplate-based, Explicit Mapping, ...).
/// </remarks>
public interface IScreenViewResolver
{
    /// <summary>
    /// إيجاد الـ View المناسب للـ ViewModel.
    /// </summary>
    /// <param name="viewModel">كائن الـ ViewModel.</param>
    /// <returns>FrameworkElement (يُستخدم كـ View)، أو null إن لم يوجد.</returns>
    object? ResolveView(object viewModel);

    /// <summary>
    /// تسجيل ربط صريح بين نوع ViewModel ونوع View.
    /// </summary>
    void RegisterMapping(Type viewModelType, Type viewType);
}