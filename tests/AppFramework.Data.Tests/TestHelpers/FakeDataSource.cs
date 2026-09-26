using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Services;

namespace AppFramework.Data.Tests.TestHelpers;

/// <summary>
/// مصدر بيانات وهمي للاختبارات — يخزّن في الذاكرة.
/// </summary>
public sealed class FakeDataSource : IDataSource
{
    private readonly Dictionary<string, object> _items = new();
    private readonly Dictionary<string, int> _saveCounts = new();

    public string Key { get; }

    public bool IsOffline { get; set; }
    public int SaveItemCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }
    public int GetBatchCallCount { get; private set; }

    public FakeDataSource(string key)
    {
        Key = key;
    }

    // =============== Test Utilities ===============

    public void Seed(params object[] items)
    {
        foreach (var item in items)
        {
            var key = ExtractKey(item);
            if (!string.IsNullOrEmpty(key))
                _items[key] = item;
        }
    }

    public int Count => _items.Count;

    public void Clear() => _items.Clear();

    // =============== IDataSource ===============

    public Task<PagedResult<ItemKey>> GetKeysAsync(DataRequest request, CancellationToken ct = default)
    {
        ThrowIfOffline();

        var keys = _items.Keys
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(k => new ItemKey(k))
            .ToList();

        return Task.FromResult(new PagedResult<ItemKey>(keys, _items.Count, request.Page, request.PageSize));
    }

    public Task<object?> GetItemAsync(string itemKey, DataRequest request, CancellationToken ct = default)
    {
        ThrowIfOffline();
        _items.TryGetValue(itemKey, out var item);
        return Task.FromResult(item);
    }

    public Task<PagedResult<object>> GetBatchAsync(DataRequest request, CancellationToken ct = default)
    {
        GetBatchCallCount++;
        ThrowIfOffline();

        var items = _items.Values
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<object>(items, _items.Count, request.Page, request.PageSize));
    }

    public Task SaveItemAsync(object item, CancellationToken ct = default)
    {
        SaveItemCallCount++;
        ThrowIfOffline();

        var key = ExtractKey(item);
        if (!string.IsNullOrEmpty(key))
        {
            _items[key] = item;
            _saveCounts[key] = _saveCounts.GetValueOrDefault(key) + 1;
        }

        return Task.CompletedTask;
    }

    public Task SaveBatchAsync(IReadOnlyList<object> items, CancellationToken ct = default)
    {
        ThrowIfOffline();

        foreach (var item in items)
        {
            var key = ExtractKey(item);
            if (!string.IsNullOrEmpty(key))
                _items[key] = item;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string itemKey, CancellationToken ct = default)
    {
        DeleteCallCount++;
        ThrowIfOffline();
        _items.Remove(itemKey);
        return Task.CompletedTask;
    }

    // =============== Helpers ===============

    private void ThrowIfOffline()
    {
        if (IsOffline)
            throw new System.Net.Http.HttpRequestException("Simulated offline");
    }

    private static string? ExtractKey(object item)
    {
        var type = item.GetType();
        var prop = type.GetProperty("Key")
                ?? type.GetProperty("Id")
                ?? type.GetProperty("ItemKey");
        return prop?.GetValue(item)?.ToString();
    }
}