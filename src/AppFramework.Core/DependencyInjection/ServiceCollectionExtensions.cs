using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Services;
using AppFramework.Core.Navigation;
using AppFramework.Core.Scanning;
using AppFramework.Core.Services;
using AppFramework.Core.ViewTemplates;

namespace AppFramework.Core.DependencyInjection;

/// <summary>
/// امتدادات <see cref="IServiceCollection"/> لتسجيل خدمات الإطار.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل الخدمات الأساسية للإطار
    /// (OperationBus, MenuManager, Navigation, Session, Scanner).
    /// </summary>
    public static IServiceCollection AddAppFrameworkCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // === Core Services ===
        services.AddSingleton<IOperationBus, OperationBus>();
        services.AddSingleton<IMenuManager, MenuManager>();

        // === Navigation ===
        services.AddSingleton<ScreenRegistry>();
        services.AddSingleton<IScreenViewResolver, WpfScreenViewResolver>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INavigationSessionService, JsonNavigationSessionService>();

        // === View Templates ===
        services.AddSingleton<IViewTemplateRegistry, ViewTemplateRegistry>();
        services.AddSingleton<IViewTemplateHost, ViewTemplateHost>();
        services.AddSingleton<IViewModePersistence, JsonViewModePersistence>();

        // === Scanning ===
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
            // بنية مؤقتة لاستخراج LoggerFactory للتسجيل
            var provider = services.BuildServiceProvider();
            var loggerFactory = provider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<AssemblyScanner>();

            var scanner = new AssemblyScanner(services, logger);
            var result = scanner.Scan(assembliesToScan);

            // سجّل الشاشات المكتشفة في ScreenRegistry
            RegisterScreensFromServices(services, result);
        }

        return services;
    }

    private static void RegisterScreensFromServices(IServiceCollection services, RegistrationResult result)
    {
        // ScreenRegistration تمت إضافتها كـ Singleton من قِبل AssemblyScanner
        // نحتاج فقط لتعريف ScreenRegistry ليقرأ منها.
        // (يتم تلقائيًا عبر DI عند أول طلب)
        _ = services;   // لا شيء إضافي
        _ = result;
    }
}
