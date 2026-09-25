using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AppFramework.Core.Navigation;

/// <summary>
/// تنفيذ JSON لخدمة الجلسة — يحفظ في
/// %APPDATA%\WPF.YS.Framework\{userId}\session.json
/// </summary>
public sealed class JsonNavigationSessionService : INavigationSessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<JsonNavigationSessionService> _logger;
    private readonly string _baseFolder;

    public JsonNavigationSessionService(ILogger<JsonNavigationSessionService> logger)
        : this(logger, GetDefaultBaseFolder())
    {
    }

    public JsonNavigationSessionService(ILogger<JsonNavigationSessionService> logger, string baseFolder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseFolder = baseFolder ?? throw new ArgumentNullException(nameof(baseFolder));
    }

    public async Task SaveAsync(NavigationSession session, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.UserId))
            throw new InvalidOperationException("Session.UserId cannot be empty.");

        var path = GetSessionPath(session.UserId);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(path, json, ct);

        _logger.LogInformation("Session saved to {Path}", path);
    }

    public async Task<NavigationSession?> LoadAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var path = GetSessionPath(userId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var session = JsonSerializer.Deserialize<NavigationSession>(json, JsonOptions);
            _logger.LogInformation("Session loaded from {Path}", path);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load session from {Path}", path);
            return null;
        }
    }

    public Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return Task.CompletedTask;

        var path = GetSessionPath(userId);
        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Session deleted: {Path}", path);
        }
        return Task.CompletedTask;
    }

    private string GetSessionPath(string userId)
    {
        // نظّف معرّف المستخدم من أي رموز خطيرة
        var safeUser = string.Join("_", userId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_baseFolder, safeUser, "session.json");
    }

    private static string GetDefaultBaseFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WPF.YS.Framework");
    }
}