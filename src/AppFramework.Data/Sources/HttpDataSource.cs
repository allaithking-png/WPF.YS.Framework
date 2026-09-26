using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Services;

namespace AppFramework.Data.Sources;

/// <summary>
/// مصدر بيانات عبر HTTP/REST.
/// </summary>
/// <remarks>
/// يستخدم <see cref="HttpClient"/> + System.Text.Json.
/// المسارات قابلة للتخصيص عبر <see cref="IHttpDataSourceConfig.Endpoints"/>.
/// </remarks>
public sealed class HttpDataSource : IDataSource
{
    private readonly HttpClient _http;
    private readonly IHttpDataSourceConfig _config;
    private readonly ILogger<HttpDataSource> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public HttpDataSource(
        HttpClient http,
        IHttpDataSourceConfig config,
        ILogger<HttpDataSource> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Key => _config.Key;

    // ==========================================================
    //  Reads
    // ==========================================================

    public async Task<PagedResult<ItemKey>> GetKeysAsync(
        DataRequest request,
        CancellationToken ct = default)
    {
        var url = BuildUrl(_config.Endpoints.Keys, request.Page, request.PageSize, key: null);

        try
        {
            var response = await _http.GetFromJsonAsync<PagedResult<ItemKey>>(url, JsonOptions, ct);
            return response ?? PagedResult<ItemKey>.Empty(request.Page, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch keys from {Url}", url);
            throw;
        }
    }

    public async Task<object?> GetItemAsync(
        string itemKey,
        DataRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);

        var url = BuildUrl(_config.Endpoints.Item, request.Page, request.PageSize, itemKey);

        try
        {
            using var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<object>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch item {Key} from {Url}", itemKey, url);
            throw;
        }
    }

    public async Task<PagedResult<object>> GetBatchAsync(
        DataRequest request,
        CancellationToken ct = default)
    {
        var url = BuildUrl(_config.Endpoints.Batch, request.Page, request.PageSize, key: null);

        try
        {
            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);

            // جرّب فك التسلسل كـ PagedResult<object> مباشرة
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // حالات محتملة:
            // 1) { items: [...], totalCount, page, pageSize }
            // 2) [...] (مصفوفة مباشرة)

            if (root.ValueKind == JsonValueKind.Array)
            {
                var items = new List<object>();
                foreach (var el in root.EnumerateArray())
                    items.Add(JsonSerializer.Deserialize<object>(el.GetRawText(), JsonOptions) ?? new object());

                return new PagedResult<object>(items, items.Count, request.Page, request.PageSize);
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                var items = new List<object>();
                int total = 0;
                string? continuation = null;

                if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in itemsEl.EnumerateArray())
                        items.Add(JsonSerializer.Deserialize<object>(el.GetRawText(), JsonOptions) ?? new object());
                }

                if (root.TryGetProperty("totalCount", out var totalEl) && totalEl.TryGetInt32(out var t))
                    total = t;

                if (root.TryGetProperty("continuationToken", out var tokenEl))
                    continuation = tokenEl.GetString();

                return new PagedResult<object>(items, total, request.Page, request.PageSize, continuation);
            }

            return PagedResult<object>.Empty(request.Page, request.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch batch from {Url}", url);
            throw;
        }
    }

    // ==========================================================
    //  Writes
    // ==========================================================

    public async Task SaveItemAsync(object item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var key = ExtractKey(item);
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Item must have a Key/Id property.");

        var url = BuildUrl(_config.Endpoints.Save, 0, 0, key);

        try
        {
            using var response = await _http.PutAsJsonAsync(url, item, JsonOptions, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Saved item {Key} to {Url}", key, url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save item {Key} to {Url}", key, url);
            throw;
        }
    }

    public async Task SaveBatchAsync(IReadOnlyList<object> items, CancellationToken ct = default)
    {
        if (items.Count == 0) return;

        var url = BuildUrl(_config.Endpoints.SaveBatch, 0, 0, key: null);

        try
        {
            using var response = await _http.PostAsJsonAsync(url, items, JsonOptions, ct);
            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Saved batch of {Count} items to {Url}", items.Count, url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch to {Url}", url);
            throw;
        }
    }

    public async Task DeleteAsync(string itemKey, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemKey);

        var url = BuildUrl(_config.Endpoints.Delete, 0, 0, itemKey);

        try
        {
            using var response = await _http.DeleteAsync(url, ct);

            // 404 مقبول (محذوف مسبقًا)
            if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                response.EnsureSuccessStatusCode();

            _logger.LogDebug("Deleted item {Key} at {Url}", itemKey, url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete item {Key} at {Url}", itemKey, url);
            throw;
        }
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private string BuildUrl(string template, int page, int pageSize, string? key)
    {
        return template
            .Replace("{base}", _config.BasePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
            .Replace("{page}", page.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{pageSize}", pageSize.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{key}", key ?? "", StringComparison.OrdinalIgnoreCase);
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