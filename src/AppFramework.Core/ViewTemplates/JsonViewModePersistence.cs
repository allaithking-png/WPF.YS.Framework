using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Core.ViewTemplates;

/// <summary>
/// حفظ واسترجاع آخر ViewMode لكل (مستخدم + شاشة) في ملف JSON.
/// </summary>
public interface IViewModePersistence
{
    /// <summary>قراءة آخر ViewMode محفوظ (أو null).</summary>
    ViewMode? Get(string userId, string screenId);

    /// <summary>حفظ ViewMode.</summary>
    void Set(string userId, string screenId, ViewMode mode);

    /// <summary>حفظ كل التغييرات على القرص.</summary>
    Task SaveAsync(string userId, CancellationToken ct = default);

    /// <summary>استرجاع كل التفضيلات من القرص.</summary>
    Task LoadAsync(string userId, CancellationToken ct = default);

    /// <summary>حذف تفضيلات المستخدم.</summary>
    Task DeleteAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// تنفيذ JSON لـ <see cref="IViewModePersistence"/>.
/// يحفظ في: %APPDATA%\WPF.YS.Framework\{userId}\viewmode.json
/// </summary>
public sealed class JsonViewModePersistence : IViewModePersistence
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<JsonViewModePersistence> _logger;
    private readonly string _baseFolder;

    // userId -> (screenId -> ViewMode)
    private readonly Dictionary<string, Dictionary<string, ViewMode>> _cache = new();
    private readonly object _lock = new();

    public JsonViewModePersistence(ILogger<JsonViewModePersistence> logger)
        : this(logger, GetDefaultBaseFolder())
    {
    }

    public JsonViewModePersistence(ILogger<JsonViewModePersistence> logger, string baseFolder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseFolder = baseFolder ?? throw new ArgumentNullException(nameof(baseFolder));
    }

    public ViewMode? Get(string userId, string screenId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(screenId))
            return null;

        lock (_lock)
        {
            if (_cache.TryGetValue(userId, out var screens) &&
                screens.TryGetValue(screenId, out var mode))
            {
                return mode;
            }
            return null;
        }
    }

    public void Set(string userId, string screenId, ViewMode mode)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(screenId))
            return;

        lock (_lock)
        {
            if (!_cache.TryGetValue(userId, out var screens))
            {
                screens = new Dictionary<string, ViewMode>();
                _cache[userId] = screens;
            }

            screens[screenId] = mode;
        }
    }

    public async Task SaveAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        Dictionary<string, ViewMode>? snapshot;
        lock (_lock)
        {
            if (!_cache.TryGetValue(userId, out var screens)) return;
            snapshot = new Dictionary<string, ViewMode>(screens);
        }

        var path = GetPath(userId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);

        _logger.LogInformation("View modes saved: {Path}", path);
    }

    public async Task LoadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var path = GetPath(userId);
        if (!File.Exists(path)) return;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var dict = JsonSerializer.Deserialize<Dictionary<string, ViewMode>>(json, JsonOptions);

            if (dict is not null)
            {
                lock (_lock)
                {
                    _cache[userId] = dict;
                }
                _logger.LogInformation("View modes loaded: {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load view modes from {Path}", path);
        }
    }

    public Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;

        var path = GetPath(userId);

        lock (_lock)
        {
            _cache.Remove(userId);
        }

        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                _logger.LogInformation("View modes deleted: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete view modes: {Path}", path);
            }
        }

        return Task.CompletedTask;
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private string GetPath(string userId)
    {
        var safeUser = string.Join("_", userId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_baseFolder, safeUser, "viewmode.json");
    }

    private static string GetDefaultBaseFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WPF.YS.Framework");
    }
}