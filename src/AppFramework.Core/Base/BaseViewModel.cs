using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Contracts;

namespace AppFramework.Core.Base;

/// <summary>
/// أساس اختياري لكل ViewModel.
/// يوفّر وصولًا سريعًا للخدمات ويستدعي AttachServices تلقائيًا.
/// </summary>
/// <remarks>
/// هذا الكلاس **اختياري**. يمكن لأي ViewModel أن يرث من
/// <see cref="ObservableObject"/> مباشرة وينفّذ <see cref="IAppAware"/> يدويًا.
/// </remarks>
public abstract class BaseViewModel : ObservableObject, IAppAware
{
    private IServiceProvider? _services;

    /// <summary>مزوّد الخدمات (متاح بعد AttachServices).</summary>
    protected IServiceProvider Services
        => _services ?? throw new InvalidOperationException(
            $"{GetType().Name}: AttachServices has not been called. " +
            "Ensure the object is created via DI or AttachServices() is invoked.");

    /// <summary>يُستدعى مرة واحدة عند إنشاء العنصر.</summary>
    public virtual void AttachServices(IServiceProvider services)
    {
        _services = services;
        OnServicesAttached();
    }

    /// <summary>نقطة تمديد للفئات المشتقة بعد ربط الخدمات.</summary>
    protected virtual void OnServicesAttached() { }

    /// <summary>اختصار للحصول على خدمة (يرمي استثناء إن لم تكن مسجّلة).</summary>
    protected T GetService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    /// <summary>اختصار للحصول على خدمة (يعيد null إن لم تكن مسجّلة).</summary>
    protected T? TryGetService<T>() where T : class
        => Services.GetService<T>();
}