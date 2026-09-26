using System;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Controls.Menus;
using AppFramework.Controls.Theming;

namespace AppFramework.Controls.DependencyInjection;

/// <summary>
/// امتدادات DI لطبقة Controls.
/// </summary>
public static class ControlsServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل مزوّدي القوائم + خدمة الثيمات.
    /// </summary>
    public static IServiceCollection AddAppFrameworkControls(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Menu Providers
        services.AddSingleton<MenuProviderRegistry>(sp =>
        {
            var registry = new MenuProviderRegistry();
            registry.Register(new MenuBarProvider());
            registry.Register(new ToolbarProvider());
            registry.Register(new RibbonProvider());
            return registry;
        });

        services.AddSingleton<MenuViewService>();

        // Theming                                                          // ← جديد
        services.AddSingleton<IThemeService, ThemeManager>();

        return services;
    }
}
