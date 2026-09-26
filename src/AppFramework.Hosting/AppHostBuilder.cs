using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AppFramework.Controls.DependencyInjection;
using AppFramework.Core.DependencyInjection;
using AppFramework.Data.DependencyInjection;
using AppFramework.Sync.DependencyInjection;
using AppFramework.Sync.Services;

namespace AppFramework.Hosting;

/// <summary>
/// باني موحّد لتطبيقات WPF.YS.Framework.
/// </summary>
/// <remarks>
/// يجمع كل التسجيلات (Core + Data + Sync + Controls) في مكان واحد.
/// <code>
/// var builder = new AppHostBuilder(args)
///     .AddFramework(f =>
///     {
///         f.ConnectionString = "Data Source=app.db";
///         f.AutoScanAssemblies.Add(typeof(App).Assembly);
///     });
///
/// await AppHost.StartAsync(builder);
/// </code>
/// </remarks>
public sealed class AppHostBuilder
{
    private readonly HostApplicationBuilder _inner;
    private readonly FrameworkOptions _frameworkOptions = new();

    public AppHostBuilder(string[] args)
    {
        _inner = Host.CreateApplicationBuilder(args);
    }

    /// <summary>الوصول إلى IServiceCollection مباشرة.</summary>
    public IServiceCollection Services => _inner.Services;

    /// <summary>وصول إلى Configuration.</summary>
    public Microsoft.Extensions.Configuration.ConfigurationManager Configuration
        => _inner.Configuration;

    // ==========================================================
    //  Configure
    // ==========================================================

    public AppHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_inner.Services);
        return this;
    }

    public AppHostBuilder ConfigureHost(Action<HostApplicationBuilder> configure)
    {
        configure(_inner);
        return this;
    }

    // ==========================================================
    //  Add Framework
    // ==========================================================

    /// <summary>
    /// تسجيل كل خدمات الإطار + Auto-scan.
    /// </summary>
    public AppHostBuilder AddFramework(Action<FrameworkOptions>? configure = null)
    {
        configure?.Invoke(_frameworkOptions);

        var services = _inner.Services;

        // 1) Core (Menu, Navigation, OperationBus, ViewTemplates, DI Scanner)
        services.AddAppFrameworkCore();

        // 2) Data (SQLite + Outbox + DataService)
        services.AddAppFrameworkData(_frameworkOptions.ConnectionString);

        // 3) Sync (Connectivity + SyncService + BackgroundService)
        services.AddAppFrameworkSync(syncOptions =>
        {
            syncOptions.Enabled = _frameworkOptions.EnableSync;
            syncOptions.Interval = _frameworkOptions.SyncInterval;
            syncOptions.SyncOnStartup = _frameworkOptions.SyncOnStartup;
        });

        // 4) Controls (Menu Providers + Themes)
        services.AddAppFrameworkControls();

        // 5) Auto-scan للمشروع (Assemblies)
        if (_frameworkOptions.AutoScanAssemblies.Count > 0)
        {
            var loggerFactory = LoggerFactory.Create(b => b.AddDebug());
            var logger = loggerFactory.CreateLogger<Core.Scanning.AssemblyScanner>();

            var scanner = new Core.Scanning.AssemblyScanner(services, logger);
            scanner.Scan(_frameworkOptions.AutoScanAssemblies.ToArray());

            // سجّل الـ ViewTemplates المكتشفة تلقائيًا
            var viewTemplateScanner = new Scanning.ViewTemplateScanner(services, logger);
            viewTemplateScanner.Scan(_frameworkOptions.AutoScanAssemblies.ToArray());
        }

        return this;
    }

    // ==========================================================
    //  Build
    // ==========================================================

    public IHost Build() => _inner.Build();
}

/// <summary>خيارات الإطار.</summary>
public sealed class FrameworkOptions
{
    /// <summary>سلسلة اتصال SQLite (null = افتراضي في %APPDATA%).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Assemblies التي يُمسح فيها Attributes.</summary>
    public List<Assembly> AutoScanAssemblies { get; } = new();

    /// <summary>هل المزامنة مفعّلة؟</summary>
    public bool EnableSync { get; set; } = true;

    /// <summary>الفاصل بين مزامنتين.</summary>
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>هل تُزامَن فور بدء التطبيق؟</summary>
    public bool SyncOnStartup { get; set; } = true;
}