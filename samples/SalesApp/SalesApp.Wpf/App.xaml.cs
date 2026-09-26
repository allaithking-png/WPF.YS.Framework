using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Theming;
using AppFramework.Core.ViewTemplates;
using AppFramework.Hosting;

namespace SalesApp.Wpf;

public partial class App : Application
{
    private static readonly string[] Args = Array.Empty<string>();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ============ 1) هيّئ الـ AppHost ============
        var builder = new AppHostBuilder(Args)
            .AddFramework(f =>
            {
                // قاعدة بيانات محلية
                f.ConnectionString = null;   // الافتراضي في %APPDATA%
                f.EnableSync = true;
                f.SyncInterval = TimeSpan.FromMinutes(2);
                f.SyncOnStartup = false;      // لا نُزامن عند البدء في الـ Demo

                // امسح Assemblies
                f.AutoScanAssemblies.Add(typeof(App).Assembly);
            })
            .ConfigureServices(services =>
            {
                // سجّل ShellViewModel + MainWindow
                services.AddSingleton<ViewModels.ShellViewModel>();
                services.AddSingleton<MainWindow>();

                // Screens
                services.AddSingleton<ViewModels.OrdersViewModel>();
                services.AddSingleton<ViewModels.ProductsViewModel>();

                // Data Sources
                services.AddSingleton<Data.OrdersDataSource>();
                services.AddSingleton<Data.ProductsDataSource>();

                // ثبت مصادر البيانات في IDataService
                services.AddSingleton<AppFramework.Abstractions.Services.IDataSource>(sp => sp.GetRequiredService<Data.OrdersDataSource>());
                services.AddSingleton<AppFramework.Abstractions.Services.IDataSource>(sp => sp.GetRequiredService<Data.ProductsDataSource>());
            });

        try
        {
            await AppHost.StartAsync(builder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"فشل تشغيل التطبيق:\n{ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // ============ 2) طبّق ثيم محفوظ ============
        var themeService = AppHost.GetService<IThemeService>();
        themeService.ApplySavedTheme("demo-user");

        // ============ 3) اعرض الـ Window ============
        var main = AppHost.GetService<MainWindow>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            // احفظ الثيم
            var themeService = AppHost.Services.GetService<IThemeService>();
            themeService?.SaveCurrentTheme("demo-user");

            await AppHost.StopAsync();
        }
        catch { /* تجاهل */ }

        base.OnExit(e);
    }
}
