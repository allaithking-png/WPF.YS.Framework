using System;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Controls.Menus;

namespace AppFramework.Controls.DependencyInjection;

/// <summary>
/// امتدادات DI لتسجيل خدمات Controls.
/// </summary>
public static class ControlsServiceCollectionExtensions
{
    /// <summary>
    /// تسجيل مزوّدي القوائم الافتراضيين (MenuBar, Toolbar, Ribbon).
    /// </summary>
    public static IServiceCollection AddAppFrameworkControls(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Registry كـ Singleton
        services.AddSingleton<MenuProviderRegistry>(sp =>
        {
            var registry = new MenuProviderRegistry();
            registry.Register(new MenuBarProvider());
            registry.Register(new ToolbarProvider());
            registry.Register(new RibbonProvider());
            return registry;
        });

        // خدمة مساعدة
        services.AddSingleton<MenuViewService>();

        return services;
    }
}