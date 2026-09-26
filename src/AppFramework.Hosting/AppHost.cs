using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AppFramework.Core.DependencyInjection;

namespace AppFramework.Hosting;

/// <summary>
/// مدخل موحّد لتشغيل التطبيق وإيقافه.
/// </summary>
public static class AppHost
{
    private static IHost? _host;

    /// <summary>هل الـ Host مشغّل؟</summary>
    public static bool IsRunning => _host is not null;

    /// <summary>مزوّد الخدمات (متاح بعد StartAsync).</summary>
    public static IServiceProvider Services
        => _host?.Services ?? throw new InvalidOperationException(
            "AppHost is not running. Call StartAsync first.");

    /// <summary>الـ Host نفسه.</summary>
    public static IHost Current => _host ?? throw new InvalidOperationException(
        "AppHost is not running. Call StartAsync first.");

    // ==========================================================
    //  Start / Stop
    // ==========================================================

    public static async Task StartAsync(AppHostBuilder builder, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (_host is not null) throw new InvalidOperationException("AppHost already started.");

        _host = builder.Build();

        // ✅ 1) طبّق ViewTemplates
        Scanning.ViewTemplateScanner.ApplyTo(_host.Services);

        // ✅ 2) فعّل DataSources في DataService
        var dataService = _host.Services.GetService<AppFramework.Abstractions.Services.IDataService>();
        var sources = _host.Services.GetServices<AppFramework.Abstractions.Services.IDataSource>();
        if (dataService is not null)
        {
            foreach (var src in sources)
                dataService.RegisterSource(src);
        }

        // 3) هيّئ AppServices
        AppServices.Initialize(_host.Services);

        await _host.StartAsync(ct);

        var logger = _host.Services.GetService<ILoggerFactory>()?.CreateLogger("AppFramework.Hosting");
        logger?.LogInformation("AppHost started");
    }
    public static async Task StopAsync(CancellationToken ct = default)
    {
        if (_host is null) return;

        try
        {
            await _host.StopAsync(ct);
        }
        catch (Exception ex)
        {
            var logger = _host.Services.GetService<ILoggerFactory>()?
                .CreateLogger("AppFramework.Hosting");
            logger?.LogError(ex, "Error stopping AppHost");
        }
        finally
        {
            _host.Dispose();
            _host = null;
        }
    }

    /// <summary>وصول مبسّط لخدمة.</summary>
    public static T GetService<T>() where T : notnull
        => Services.GetRequiredService<T>();
}
