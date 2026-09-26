using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Services;
using AppFramework.Data.Storage;

namespace AppFramework.Data.Services;

/// <summary>
/// تنفيذ <see cref="IDataService"/> بنمط Offline-first.
/// </summary>
/// <remarks>
/// - القراءة: يقرأ من المحلي أولًا، ثم يُحدّث من السيرفر في الخلفية.
/// - الكتابة: تُسجَّل في Outbox ثم تُرسل لاحقًا (Write-Behind).
/// - الاشتراكات: يوزّع تغييرات المصادر على المشتركين.
/// </remarks>
public sealed class DataService : IDataService
{
    private readonly ILocalStore _localStore;
    private readonly ILogger<DataService> _logger;

    private readonly ConcurrentDictionary<string, IDataSource> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<Func<DataChange, Task>>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    public DataService(
        ILocalStore localStore,
        ILogger<DataService> logger)
    {
        _localStore = localStore ?? throw new ArgumentNullException(nameof(localStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==========================================================
    //  Registration
    // ==========================================================

    public void RegisterSource(IDataSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _sources[source.Key] = source;
        _logger.LogInformation("Data source registered: {Key}", source.Key);
    }

    public IDataSource GetSource(string key)
    {
        if (TryGetSource(key, out var source) && source is not null)
            return source;

        throw new InvalidOperationException($"Data source '{key}' is not registered.");
    }

    public bool TryGetSource(string key, out IDataSource? source)
    {
        if (string.IsNullOrEmpty(key))
        {
            source = null;
            return false;
        }
        return _sources.TryGetValue(key, out source);
    }

    // ==========================================================
    //  Reads — Offline-first
    // ==========================================================

    public async Task<PagedResult<object>> LoadBatchAsync(
        DataRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1) اقرأ من المحلي أولًا (سريع)
        var local = await _localStore.QueryAsync(request, ct);

        // 2) إن وُجد محلي -> أعِد فورًا + حدّث في الخلفية
        if (local.Items.Count > 0)
        {
            _ = RefreshInBackgroundAsync(request, ct);
            return local;
        }

        // 3) لم يوجد محلي -> اذهب للسيرفر
        if (!TryGetSource(request.SourceKey, out var source) || source is null)
            return PagedResult<object>.Empty(request.Page, request.PageSize);

        try
        {
            var remote = await source.GetBatchAsync(request, ct);

            if (remote.Items.Count > 0)
                await _localStore.UpsertBatchAsync(request.SourceKey, remote.Items, ct);

            return remote;
        }
        catch (Exception ex) when (IsOfflineException(ex))
        {
            _logger.LogWarning(ex, "Offline — empty result for {Source}", request.SourceKey);
            return PagedResult<object>.Empty(request.Page, request.PageSize);
        }
    }

    public async Task<PagedResult<object>> LoadBatchLocalFirstAsync(
        DataRequest request,
        CancellationToken ct = default)
        => await LoadBatchAsync(request, ct);

    public async Task<PagedResult<ItemKey>> LoadKeysAsync(
        DataRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var local = await _localStore.QueryKeysAsync(request, ct);
        if (local.Items.Count > 0) return local;

        if (!TryGetSource(request.SourceKey, out var source) || source is null)
            return PagedResult<ItemKey>.Empty(request.Page, request.PageSize);

        try
        {
            return await source.GetKeysAsync(request, ct);
        }
        catch (Exception ex) when (IsOfflineException(ex))
        {
            _logger.LogWarning(ex, "Offline — no keys for {Source}", request.SourceKey);
            return PagedResult<ItemKey>.Empty(request.Page, request.PageSize);
        }
    }

    public async Task<object?> LoadItemAsync(
        string sourceKey,
        string itemKey,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);

        var local = await _localStore.GetItemAsync(sourceKey, itemKey, ct);
        if (local is not null) return local;

        if (!TryGetSource(sourceKey, out var source) || source is null)
            return null;

        try
        {
            var remote = await source.GetItemAsync(itemKey, new DataRequest(sourceKey), ct);
            if (remote is not null)
                await _localStore.UpsertBatchAsync(sourceKey, new[] { remote }, ct);

            return remote;
        }
        catch (Exception ex) when (IsOfflineException(ex))
        {
            _logger.LogWarning(ex, "Offline — no item {Key} for {Source}", itemKey, sourceKey);
            return null;
        }
    }

    // ==========================================================
    //  Writes — Write-Behind
    // ==========================================================

    public async Task SaveItemAsync(
        string sourceKey,
        object item,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(item);

        var itemKey = ExtractKey(item);

        // 1) Outbox
        await _localStore.EnqueueChangeAsync(
            new DataChange(sourceKey, itemKey, DataChangeKind.Updated, item), ct);

        // 2) محلي
        await _localStore.UpsertBatchAsync(sourceKey, new[] { item }, ct);

        // 3) أبلغ المشتركين
        PublishChange(new DataChange(sourceKey, itemKey, DataChangeKind.Updated, item));

        // 4) حاول الإرسال الفوري
        _ = TryFlushOutboxAsync(sourceKey, ct);
    }

    public async Task SaveBatchAsync(
        string sourceKey,
        IReadOnlyList<object> items,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0) return;

        await _localStore.EnqueueChangeAsync(
            new DataChange(sourceKey, null, DataChangeKind.Refreshed, items.ToList()), ct);

        await _localStore.UpsertBatchAsync(sourceKey, items, ct);

        PublishChange(new DataChange(sourceKey, null, DataChangeKind.Refreshed));

        _ = TryFlushOutboxAsync(sourceKey, ct);
    }

    public async Task DeleteAsync(
        string sourceKey,
        string itemKey,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);

        await _localStore.EnqueueChangeAsync(
            new DataChange(sourceKey, itemKey, DataChangeKind.Deleted), ct);

        await _localStore.RemoveAsync(sourceKey, itemKey, ct);

        PublishChange(new DataChange(sourceKey, itemKey, DataChangeKind.Deleted));

        _ = TryFlushOutboxAsync(sourceKey, ct);
    }

    // ==========================================================
    //  Subscriptions
    // ==========================================================

    public IDisposable Subscribe(string sourceKey, Func<DataChange, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        ArgumentNullException.ThrowIfNull(handler);

        var list = _subscribers.GetOrAdd(sourceKey, _ => new List<Func<DataChange, Task>>());

        lock (list) list.Add(handler);

        return new Subscription(() =>
        {
            lock (list) list.Remove(handler);
        });
    }

    public void PublishChange(DataChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (!_subscribers.TryGetValue(change.SourceKey, out var list))
            return;

        Func<DataChange, Task>[] snapshot;
        lock (list) snapshot = list.ToArray();

        foreach (var handler in snapshot)
            _ = SafeInvokeAsync(handler, change);
    }

    // ==========================================================
    //  Internal
    // ==========================================================

    private async Task RefreshInBackgroundAsync(DataRequest request, CancellationToken ct)
    {
        if (!TryGetSource(request.SourceKey, out var source) || source is null) return;

        try
        {
            var remote = await source.GetBatchAsync(request, ct);
            if (remote.Items.Count > 0)
            {
                await _localStore.UpsertBatchAsync(request.SourceKey, remote.Items, ct);
                PublishChange(new DataChange(request.SourceKey, null, DataChangeKind.Refreshed));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Background refresh failed for {Source}", request.SourceKey);
        }
    }

    private async Task TryFlushOutboxAsync(string sourceKey, CancellationToken ct)
    {
        if (!TryGetSource(sourceKey, out var source) || source is null) return;

        try
        {
            var pending = await _localStore.DequeuePendingAsync(batchSize: 50, ct);
            if (pending.Count == 0) return;

            var succeeded = new List<DataChange>(pending.Count);

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
                                await source.SaveItemAsync(change.Payload, ct);
                            break;

                        case DataChangeKind.Deleted:
                            if (!string.IsNullOrEmpty(change.ItemKey))
                                await source.DeleteAsync(change.ItemKey, ct);
                            break;

                        case DataChangeKind.Refreshed:
                            if (change.Payload is IEnumerable<object> batch)
                                await source.SaveBatchAsync(batch.ToList(), ct);
                            break;
                    }

                    succeeded.Add(change);
                }
                catch (Exception ex)
                {
                    await _localStore.MarkFailedAsync(sourceKey, change.ItemKey, ex.Message, ct);
                    _logger.LogWarning(ex, "Failed to sync change for {Source}", sourceKey);
                }
            }

            if (succeeded.Count > 0)
            {
                await _localStore.MarkSyncedAsync(succeeded, ct);
                _logger.LogDebug("Synced {Count} changes for {Source}", succeeded.Count, sourceKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Outbox flush failed for {Source}", sourceKey);
        }
    }

    private static async Task SafeInvokeAsync(Func<DataChange, Task> handler, DataChange change)
    {
        try { await handler(change); }
        catch { /* تجاهل */ }
    }

    private static bool IsOfflineException(Exception ex)
        => ex is HttpRequestException
        or TaskCanceledException
        or TimeoutException
        or System.Net.Sockets.SocketException;

    private static string? ExtractKey(object item)
    {
        var type = item.GetType();
        var prop = type.GetProperty("Key")
                ?? type.GetProperty("Id")
                ?? type.GetProperty("ItemKey");

        return prop?.GetValue(item)?.ToString();
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public Subscription(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dispose();
        }
    }
}