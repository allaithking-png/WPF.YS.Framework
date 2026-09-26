using System;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppFramework.Sync.Services;

/// <summary>
/// خلفية تعمل مع الـ Host — تشغّل المزامنة الدورية وتراقب الاتصال.
/// </summary>
public sealed class SyncBackgroundService : BackgroundService
{
    private readonly ISyncService _syncService;
    private readonly Connectivity.IConnectivityMonitor _connectivity;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncBackgroundService> _logger;

    public SyncBackgroundService(
        ISyncService syncService,
        Connectivity.IConnectivityMonitor connectivity,
        IOptions<SyncOptions> options,
        ILogger<SyncBackgroundService> logger)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _options = options?.Value ?? new SyncOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SyncBackgroundService starting: Enabled={Enabled}, Interval={Interval}",
            _options.Enabled, _options.Interval);

        if (!_options.Enabled)
        {
            _logger.LogInformation("Sync is disabled by configuration");
            return;
        }

        // 1) ابدأ مراقبة الاتصال
        _connectivity.Start();

        // 2) شغّل المزامنة التلقائية
        _syncService.StartAutoSync(_options.Interval);

        // 3) مزامنة أولية فورية (اختياري)
        if (_options.SyncOnStartup && _connectivity.IsOnline)
        {
            try
            {
                await _syncService.SyncAllAsync(stoppingToken);
                _logger.LogInformation("Initial sync completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial sync failed");
            }
        }

        // 4) انتظر إيقاف التطبيق
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // طبيعي عند الإغلاق
        }
        finally
        {
            _logger.LogInformation("SyncBackgroundService stopping");
            _syncService.StopAutoSync();
            _connectivity.Stop();
        }
    }
}

/// <summary>
/// إعدادات المزامنة.
/// </summary>
public sealed class SyncOptions
{
    /// <summary>هل المزامنة مفعّلة؟</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>الفاصل الزمني بين مزامنتين تلقائيتين.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>هل تُزامَن فور بدء التطبيق؟</summary>
    public bool SyncOnStartup { get; set; } = true;
}
