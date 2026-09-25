using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Data.Sqlite;
using AppFramework.Data.Sqlite.Entities;

namespace AppFramework.Data.Storage;

/// <summary>
/// تنفيذ <see cref="ILocalStore"/> على SQLite عبر EF Core.
/// </summary>
public sealed class SqliteLocalStore : ILocalStore
{
    private readonly IDbContextFactory<LocalDbContext> _factory;
    private readonly ILogger<SqliteLocalStore> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SqliteLocalStore(
        IDbContextFactory<LocalDbContext> factory,
        ILogger<SqliteLocalStore> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ==========================================================
    //  Reads
    // ==========================================================

    public async Task<PagedResult<object>> QueryAsync(DataRequest request, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.Records
            .AsNoTracking()
            .Where(r => r.SourceKey == request.SourceKey)
            .OrderByDescending(r => r.UpdatedAt);

        var total = await query.CountAsync(ct);

        var records = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = new List<object>(records.Count);
        foreach (var rec in records)
        {
            var obj = Deserialize(rec);
            if (obj is not null) items.Add(obj);
        }

        return new PagedResult<object>(items, total, request.Page, request.PageSize);
    }

    public async Task<PagedResult<ItemKey>> QueryKeysAsync(DataRequest request, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.Records
            .AsNoTracking()
            .Where(r => r.SourceKey == request.SourceKey)
            .OrderByDescending(r => r.UpdatedAt);

        var total = await query.CountAsync(ct);

        var keys = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ItemKey(r.ItemKey, null, null))
            .ToListAsync(ct);

        return new PagedResult<ItemKey>(keys, total, request.Page, request.PageSize);
    }

    public async Task<object?> GetItemAsync(string sourceKey, string itemKey, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var rec = await db.Records
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SourceKey == sourceKey && r.ItemKey == itemKey, ct);

        return rec is null ? null : Deserialize(rec);
    }

    // ==========================================================
    //  Writes
    // ==========================================================

    public async Task UpsertBatchAsync(
        string sourceKey,
        IReadOnlyList<object> items,
        CancellationToken ct = default)
    {
        if (items.Count == 0) return;

        await using var db = await _factory.CreateDbContextAsync(ct);

        // اجمع المفاتيح الموجودة مسبقًا
        var keys = items
            .Select(ExtractKey)
            .Where(k => k is not null)
            .Cast<string>()
            .ToList();

        var existing = await db.Records
            .Where(r => r.SourceKey == sourceKey && keys.Contains(r.ItemKey))
            .ToDictionaryAsync(r => r.ItemKey, ct);

        foreach (var item in items)
        {
            var key = ExtractKey(item);
            if (string.IsNullOrEmpty(key)) continue;

            var type = item.GetType();
            var payload = JsonSerializer.Serialize(item, type, JsonOptions);
            var clrType = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

            if (existing.TryGetValue(key, out var rec))
            {
                rec.Payload = payload;
                rec.ClrType = clrType;
                rec.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                db.Records.Add(new LocalRecord
                {
                    Id = Guid.NewGuid(),
                    SourceKey = sourceKey,
                    ItemKey = key,
                    ClrType = clrType,
                    Payload = payload,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogDebug("Upserted {Count} items for source {Source}", items.Count, sourceKey);
    }

    public async Task RemoveAsync(string sourceKey, string itemKey, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var rec = await db.Records
            .FirstOrDefaultAsync(r => r.SourceKey == sourceKey && r.ItemKey == itemKey, ct);

        if (rec is not null)
        {
            db.Records.Remove(rec);
            await db.SaveChangesAsync(ct);
            _logger.LogDebug("Removed item {Key} from source {Source}", itemKey, sourceKey);
        }
    }

    // ==========================================================
    //  Outbox
    // ==========================================================

    public async Task EnqueueChangeAsync(DataChange change, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        string? payload = null;
        string? clrType = null;

        if (change.Payload is not null)
        {
            var type = change.Payload.GetType();
            payload = JsonSerializer.Serialize(change.Payload, type, JsonOptions);
            clrType = type.AssemblyQualifiedName;
        }

        db.Outbox.Add(new OutboxEntry
        {
            Id = Guid.NewGuid(),
            SourceKey = change.SourceKey,
            ItemKey = change.ItemKey,
            Kind = change.Kind,
            Payload = payload,
            ClrType = clrType,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DataChange>> DequeuePendingAsync(
        int batchSize,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entries = await db.Outbox
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        var result = new List<DataChange>(entries.Count);
        foreach (var e in entries)
        {
            object? payload = null;
            if (e.Payload is not null && e.ClrType is not null)
            {
                var type = Type.GetType(e.ClrType);
                if (type is not null)
                    payload = JsonSerializer.Deserialize(e.Payload, type, JsonOptions);
            }

          result.Add(new DataChange(
    e.SourceKey,
    e.ItemKey,
    e.Kind,
    payload,
    new DateTimeOffset(e.CreatedAt, TimeSpan.Zero)));
        }

        return result;
    }

    public async Task MarkSyncedAsync(
        IEnumerable<DataChange> changes,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        // نحذف مدخلات Outbox المطابقة
        foreach (var change in changes)
        {
            var entry = await db.Outbox
                .Where(e => e.SourceKey == change.SourceKey
                         && e.ItemKey == change.ItemKey
                         && e.Kind == change.Kind)
                .OrderBy(e => e.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (entry is not null)
                db.Outbox.Remove(entry);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(
        string sourceKey,
        string? itemKey,
        string error,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entry = await db.Outbox
            .Where(e => e.SourceKey == sourceKey && e.ItemKey == itemKey)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (entry is not null)
        {
            entry.Attempts++;
            entry.LastError = error;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetPendingCountAsync(string? sourceKey = null, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var query = db.Outbox.AsQueryable();
        if (!string.IsNullOrEmpty(sourceKey))
            query = query.Where(e => e.SourceKey == sourceKey);

        return await query.CountAsync(ct);
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private object? Deserialize(LocalRecord rec)
    {
        try
        {
            var type = Type.GetType(rec.ClrType);
            if (type is null)
            {
                _logger.LogWarning("Unknown type {Type} for record {Key}", rec.ClrType, rec.ItemKey);
                return null;
            }

            return JsonSerializer.Deserialize(rec.Payload, type, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize record {Key}", rec.ItemKey);
            return null;
        }
    }

    private static string? ExtractKey(object item)
    {
        // ابحث عن خاصية "Key" أو "Id" أو "ItemKey"
        var type = item.GetType();
        var prop = type.GetProperty("Key")
                ?? type.GetProperty("Id")
                ?? type.GetProperty("ItemKey");

        return prop?.GetValue(item)?.ToString();
    }
}