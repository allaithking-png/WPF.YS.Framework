using System;
using Microsoft.Extensions.DependencyInjection;

namespace AppFramework.Core.DependencyInjection;

/// <summary>
/// نقطة وصول سريعة وعالمية لمزوّد الخدمات.
/// </summary>
/// <remarks>
/// تُهيَّأ مرة واحدة عند إقلاع التطبيق عبر <see cref="Initialize"/>.
/// بعد ذلك، يمكن استخدام <c>AppServices.Get&lt;T&gt;()</c> من أي مكان
/// (حتى من الكود الذي لا يمرّ عبر DI).
/// </remarks>
public static class AppServices
{
    private static IServiceProvider? _provider;

    /// <summary>هل تم التهيئة؟</summary>
    public static bool IsInitialized => _provider is not null;

    /// <summary>مزوّد الخدمات (يرمي إن لم يُهيَّأ).</summary>
    public static IServiceProvider Provider
        => _provider ?? throw new InvalidOperationException(
            "AppServices not initialized. Call AppServices.Initialize(services) during app startup.");

    /// <summary>تهيئة نقطة الوصول (تُستدعى مرة واحدة).</summary>
    public static void Initialize(IServiceProvider provider)
    {
        if (_provider is not null)
            throw new InvalidOperationException("AppServices already initialized.");

        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>الحصول على خدمة (يرمي إن لم تكن مسجّلة).</summary>
    public static T Get<T>() where T : notnull
        => Provider.GetRequiredService<T>();

    /// <summary>الحصول على خدمة (يعيد null إن لم تكن مسجّلة).</summary>
    public static T? TryGet<T>() where T : class
        => Provider.GetService<T>();

    /// <summary>إنشاء scope جديد (مفيد للـ Background Tasks).</summary>
    public static IServiceScope CreateScope() => Provider.CreateScope();
}