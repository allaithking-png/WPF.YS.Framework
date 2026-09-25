using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Services;
using AppFramework.Core.Scanning;
using AppFramework.Core.Services;

namespace AppFramework.Core.DependencyInjection;

/// <summary>
/// امتدادات <see cref="IServiceCollection"/> لتسجيل خدمات الإطار.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل الخدمات الأساسية للإطار (OperationBus, MenuManager, AssemblyScanner).
    /// </summary>
    public static IServiceCollection AddAppFrameworkCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // الخدمات الأساسية كـ Singleton
        services.AddSingleton<IOperationBus, OperationBus>();
        services.AddSingleton<IMenuManager, MenuManager>();

        // Scanner (يُستخدم عند الإقلاع — لا نُسجّله كـ Singleton لتفادي مشاكل دورة الحياة)
        services.AddTransient<AssemblyScanner>();

        return services;
    }

    /// <summary>
    /// تسجيل الخدمات الأساسية + مسح تلقائي للـ Assemblies.
    /// </summary>
    public static IServiceCollection AddAppFrameworkCore(
        this IServiceCollection services,
        params Assembly[] assembliesToScan)
    {
        services.AddAppFrameworkCore();

        if (assembliesToScan is { Length: > 0 })
        {
            // أنشئ Scanner مؤقتًا للتسجيل
            var provider = services.BuildServiceProvider();
            var logger = provider.GetService<ILogger<AssemblyScanner>>();
            var scanner = new AssemblyScanner(services, logger);
            scanner.Scan(assembliesToScan);
        }

        return services;
    }
}