using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Services;
using AppFramework.Data.Storage;
using AppFramework.Sync.Connectivity;

namespace AppFramework.Sync.Services;

/// <summary>
/// تنفيذ <see cref="ISyncService"/>.
/// ينسّق بين Outbox (المحلي) والمصادر (البعيدة).
/// </summary>
/// <remarks>
/// استراتيجية التعارض: Server Wins (افتراضيًا).
/// </remarks>
public sealed class SyncService : ISyncService, IDisposable
{
    private readonly IDataService _dataService;
    private readonly ILocalStore _localStore;
    private readonly IConnectivityMonitor _connectivity;
    private readonly ILogger<SyncService> _logger;

    private Timer? _autoSyncTimer;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly List<string> _knownSources = new();
    private readonly object _sourcesLock = new();

    public SyncService(
        IDataService dataService,
        ILocalStore localStore,
        IConnectivityMonitor connectivity,
        ILogger<SyncService> logger)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsOnline => _connectivity.IsOnline;

    public event EventHandler<SyncResult>? SyncCompleted;
    public event EventHandler<bool>? ConnectivityChanged;

    // ==========================================================
    //  Sync
    // ==========================================================

    public async Task<SyncResult> SyncAsync(string sourceKey, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        // منع المزامنة المتزامنة
        await _syncLock.WaitAsync(ct);
        try
        {
            if (!_connectivity.IsOnline)
            {
                _logger.LogDebug("Skipping sync for {Source}: offline", sourceKey);
                return new SyncResult(sourceKey, 0, 0, 0);
            }

            // 1) Push pending changes
            var pushed = await PushPendingAsync(sourceKey, ct);

            // 2) Pull updates from server
            var pulled = await PullRemoteAsync(sourceKey, ct);

            var result = new SyncResult(sourceKey, pushed, pulled, 0);
            SyncCompleted?.Invoke(this, result);

            if (pushed > 0 || pulled > 0)
                _logger.LogInformation("Synced {Source}: {Pushed} pushed, {Pulled} pulled",
                    sourceKey, pushed, pulled);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for {Source}", sourceKey);
            var result = new SyncResult(sourceKey, 0, 0, 0, ex);
            SyncCompleted?.Invoke(this, result);
            return result;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<SyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        var sources = GetKnownSources();
        var totalPushed = 0;
        var totalPulled = 0;
        Exception? firstError = null;

        foreach (var source in sources)
        {
            try
            {
                var result = await SyncAsync(source, ct);
                totalPushed += result.Pushed;
                totalPulled += result.Pulled;
                if (result.Error is not null && firstError is null)
                    firstError = result.Error;
            }
            catch (Exception ex)
            {
                if (firstError is null) firstError = ex;
                _logger.LogWarning(ex, "Sync failed for {Source}", source);
            }
        }

        return new SyncResult("*", totalPushed, totalPulled, 0, firstError);
    }

    // ==========================================================
    //  Auto Sync
    // ==========================================================

    public void StartAutoSync(TimeSpan interval)
    {
        StopAutoSync();

        _autoSyncTimer = new Timer(async _ =>
        {
            try
            {
                if (_connectivity.IsOnline)
                    await SyncAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-sync iteration failed");
            }
        }, null, interval, interval);

        _logger.LogInformation("Auto-sync started: interval {Interval}", interval);
    }

    public void StopAutoSync()
    {
        _autoSyncTimer?.Dispose();
        _autoSyncTimer = null;
        _logger.LogInformation("Auto-sync stopped");
    }

    // ==========================================================
    //  Source tracking
    // ==========================================================

    /// <summary>تسجيل مصدر للمزامنة.</summary>
    public void RegisterSource(string sourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        lock (_sourcesLock)
        {
            if (!_knownSources.Contains(sourceKey, StringComparer.OrdinalIgnoreCase))
                _knownSources.Add(sourceKey);
        }
    }

    private IReadOnlyList<string> GetKnownSources()
    {
        lock (_sourcesLock)
        {
            return _knownSources.ToList();
        }
    }

    // ==========================================================
    //  Internal
    // ==========================================================

    private async Task<int> PushPendingAsync(string sourceKey, CancellationToken ct)
    {
        if (!_dataService.TryGetSource(sourceKey, out var source) || source is null)
            return 0;

        var pending = await _localStore.DequeuePendingAsync(batchSize: 50, ct);
        if (pending.Count == 0) return 0;

        var succeeded = new List<DataChange>(pending.Count);
        var pushed = 0;

        foreach (var change in pending)
        {
            if (!string.Equals(change.SourceKey, sourceKey, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                switch (change.Kind)
                {
                    case DataChangeKind.Added:
                    case DataChangeKind.Updated:
                        if (change.Payload is not null)
                        {
                            await source.SaveItemAsync(change.Payload, ct);
                            pushed++;
                        }
                        break;

                    case DataChangeKind.Deleted:
                        if (!string.IsNullOrEmpty(change.ItemKey))
                        {
                            await source.DeleteAsync(change.ItemKey, ct);
                            pushed++;
                        }
                        break;

                    case DataChangeKind.Refreshed:
                        if (change.Payload is IEnumerable<object> batch)
                        {
                            var list = batch.ToList();
                            await source.SaveBatchAsync(list, ct);
                            pushed += list.Count;
                        }
                        break;
                }

                succeeded.Add(change);
            }
            catch (Exception ex)
            {
                await _localStore.MarkFailedAsync(sourceKey, change.ItemKey, ex.Message, ct);
                _logger.LogWarning(ex, "Failed to push change for {Source}", sourceKey);
            }
        }

        if (succeeded.Count > 0)
            await _localStore.MarkSyncedAsync(succeeded, ct);

        return pushed;
    }

    private async Task<int> PullRemoteAsync(string sourceKey, CancellationToken ct)
    {
        if (!_dataService.TryGetSource(sourceKey, out var source) || source is null)
            return 0;

        try
        {
            var result = await source.GetBatchAsync(
                new DataRequest(sourceKey, Page: 1, PageSize: 200,
                    Strategy: DataLoadStrategy.FirstPage), ct);

            if (result.Items.Count == 0) return 0;

            await _localStore.UpsertBatchAsync(sourceKey, result.Items, ct);
            _dataService.PublishChange(new DataChange(sourceKey, null, DataChangeKind.Refreshed));

            return result.Items.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pull from {Source}", sourceKey);
            return 0;
        }
    }

    private void OnConnectivityChanged(object? sender, bool isOnline)
    {
        ConnectivityChanged?.Invoke(this, isOnline);

        if (isOnline)
        {
            _logger.LogInformation("Connectivity restored — triggering sync");
            _ = Task.Run(async () =>
            {
                try { await SyncAllAsync(); }
                catch (Exception ex) { _logger.LogWarning(ex, "Post-reconnect sync failed"); }
            });
        }
    }

    public void Dispose()
    {
        StopAutoSync();
        _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _syncLock.Dispose();
    }
}