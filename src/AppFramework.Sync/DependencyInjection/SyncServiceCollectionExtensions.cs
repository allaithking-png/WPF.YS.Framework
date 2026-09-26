using System;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Services;
using AppFramework.Sync.Connectivity;
using AppFramework.Sync.Services;

namespace AppFramework.Sync.DependencyInjection;

/// <summary>
/// امتدادات DI لطبقة المزامنة.
/// </summary>
public static class SyncServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل محرك المزامنة (SyncService + Connectivity + Background Worker).
    /// </summary>
    /// <param name="services">حاوية الخدمات.</param>
    /// <param name="configure">تخصيص إعدادات المزامنة (اختياري).</param>
    public static IServiceCollection AddAppFrameworkSync(
        this IServiceCollection services,
        Action<SyncOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // الخيارات
        var options = new SyncOptions();
        configure?.Invoke(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // مراقب الاتصال
        services.AddSingleton<IConnectivityMonitor, NetworkConnectivityMonitor>();

        // محرك المزامنة
        services.AddSingleton<SyncService>();
        services.AddSingleton<ISyncService>(sp => sp.GetRequiredService<SyncService>());

        // خلفية المزامنة
        services.AddHostedService<SyncBackgroundService>();

        return services;
    }
}