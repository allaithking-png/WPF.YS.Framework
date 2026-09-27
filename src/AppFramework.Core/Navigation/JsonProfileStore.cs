using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Core.Navigation;

/// <summary>
/// مسؤول عن حفظ/تحميل <see cref="UserNavProfile"/> بملفين:
/// - navprofile.json (المُلتزم)
/// - navprofile.pending.json (staging)
/// </summary>
public interface IUserNavProfileStore
{
    Task<UserNavProfile?> LoadAsync(string userId, CancellationToken ct = default);
    Task SaveAsync(UserNavProfile profile, CancellationToken ct = default);

    // Staging
    Task<UserNavProfile?> LoadPendingAsync(string userId, CancellationToken ct = default);
    Task SavePendingAsync(UserNavProfile profile, CancellationToken ct = default);
    Task DeletePendingAsync(string userId, CancellationToken ct = default);

    Task DeleteAsync(string userId, CancellationToken ct = default);
}

/// <summary>
/// تنفيذ JSON لـ <see cref="IUserNavProfileStore"/>.
/// </summary>
public sealed class JsonProfileStore : IUserNavProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<JsonProfileStore> _logger;
    private readonly string _baseFolder;

    public JsonProfileStore(ILogger<JsonProfileStore> logger)
        : this(logger, GetDefaultBaseFolder()) { }

    public JsonProfileStore(ILogger<JsonProfileStore> logger, string baseFolder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseFolder = baseFolder ?? throw new ArgumentNullException(nameof(baseFolder));
    }

    // ==========================================================
    //  Main
    // ==========================================================

    public async Task<UserNavProfile?> LoadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var path = GetMainPath(userId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<UserNavProfile>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load main profile: {Path}", path);
            return null;
        }
    }

    public async Task SaveAsync(UserNavProfile profile, CancellationToken ct = default)
    {
        if (profile is null || string.IsNullOrWhiteSpace(profile.UserId)) return;

        var path = GetMainPath(profile.UserId);
        await WriteJsonAsync(path, profile, ct);
        _logger.LogDebug("Profile saved: {Path}", path);
    }

    // ==========================================================
    //  Pending (staging)
    // ==========================================================

    public async Task<UserNavProfile?> LoadPendingAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var path = GetPendingPath(userId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            return JsonSerializer.Deserialize<UserNavProfile>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load pending profile: {Path}", path);
            return null;
        }
    }

    public async Task SavePendingAsync(UserNavProfile profile, CancellationToken ct = default)
    {
        if (profile is null || string.IsNullOrWhiteSpace(profile.UserId)) return;

        var path = GetPendingPath(profile.UserId);
        await WriteJsonAsync(path, profile, ct);
    }

    public Task DeletePendingAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;

        var path = GetPendingPath(userId);
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete pending: {Path}", path);
        }

        return Task.CompletedTask;
    }

    // ==========================================================
    //  Delete
    // ==========================================================

    public Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;

        try
        {
            var main = GetMainPath(userId);
            if (File.Exists(main)) File.Delete(main);

            var pending = GetPendingPath(userId);
            if (File.Exists(pending)) File.Delete(pending);

            _logger.LogInformation("Profile deleted for user {User}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete profile for {User}", userId);
        }

        return Task.CompletedTask;
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    private async Task WriteJsonAsync(string path, object obj, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(obj, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);
    }

    private string GetMainPath(string userId)
    {
        var safe = SanitizeUser(userId);
        return Path.Combine(_baseFolder, safe, "navprofile.json");
    }

    private string GetPendingPath(string userId)
    {
        var safe = SanitizeUser(userId);
        return Path.Combine(_baseFolder, safe, "navprofile.pending.json");
    }

    private static string SanitizeUser(string userId)
        => string.Join("_", userId.Split(Path.GetInvalidFileNameChars()));

    private static string GetDefaultBaseFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WPF.YS.Framework");
    }
}