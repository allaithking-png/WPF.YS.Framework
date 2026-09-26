using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace AppFramework.Controls.Theming;

/// <summary>
/// تنفيذ <see cref="IThemeService"/>.
/// يبدّل ResourceDictionary للثيم داخل <see cref="Application.Current"/>.
/// </summary>
public sealed class ThemeManager : IThemeService
{
    private const string ThemeDictionaryMarker = "AppFramework.Theme";

    private readonly ILogger<ThemeManager> _logger;
    private readonly string _settingsFolder;

    private AppTheme _currentTheme = AppTheme.Light;

    public ThemeManager(ILogger<ThemeManager> logger)
        : this(logger, GetDefaultSettingsFolder())
    {
    }

    public ThemeManager(ILogger<ThemeManager> logger, string settingsFolder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settingsFolder = settingsFolder ?? throw new ArgumentNullException(nameof(settingsFolder));
    }

    public AppTheme CurrentTheme => _currentTheme;

    public IReadOnlyCollection<AppTheme> AvailableThemes { get; } = new[]
    {
        AppTheme.Light,
        AppTheme.Dark,
        AppTheme.Corporate
    };

    public event EventHandler<AppTheme>? ThemeChanged;

    // ==========================================================
    //  Apply
    // ==========================================================

    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            _logger.LogWarning("Cannot apply theme: Application.Current is null (design-time?)");
            return;
        }

        try
        {
            var newDictionary = LoadThemeDictionary(theme);

            // ابحث عن الـ dictionary القديم للثيم
            var existing = FindThemeDictionary(app);

            if (existing is not null)
                app.Resources.MergedDictionaries.Remove(existing);

            // أضف الجديد في البداية (ليُطبَّق أولًا)
            app.Resources.MergedDictionaries.Insert(0, newDictionary);

            // تأكد أن Controls.xaml مضاف (للـ Styles)
            EnsureControlsDictionary(app);

            _currentTheme = theme;

            _logger.LogInformation("Theme applied: {Theme}", theme);
            ThemeChanged?.Invoke(this, theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme: {Theme}", theme);
        }
    }

    public void ApplySavedTheme(string? userId = null)
    {
        var theme = LoadSavedTheme(userId);
        if (theme.HasValue)
            ApplyTheme(theme.Value);
        else
            ApplyTheme(AppTheme.Light);
    }

    public void SaveCurrentTheme(string? userId = null)
    {
        try
        {
            var path = GetSettingsPath(userId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, _currentTheme.ToString());
            _logger.LogDebug("Theme saved: {Theme}", _currentTheme);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save theme");
        }
    }

    // ==========================================================
    //  Internal
    // ==========================================================

    private AppTheme? LoadSavedTheme(string? userId)
    {
        try
        {
            var path = GetSettingsPath(userId);
            if (!File.Exists(path)) return null;

            var text = File.ReadAllText(path).Trim();
            if (Enum.TryParse<AppTheme>(text, ignoreCase: true, out var theme))
                return theme;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load saved theme");
        }

        return null;
    }

    private static ResourceDictionary LoadThemeDictionary(AppTheme theme)
    {
        // URI يتوقع أن الملفات في Themes/Skins/{theme}.xaml
        var uri = new Uri(
            $"pack://application:,,,/AppFramework.Controls;component/Themes/Skins/{theme}.xaml",
            UriKind.Absolute);

        return new ResourceDictionary { Source = uri };
    }

    private static ResourceDictionary? FindThemeDictionary(Application app)
    {
        return app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Contains(ThemeDictionaryMarker));
    }

    private static void EnsureControlsDictionary(Application app)
    {
        var controlsUri = new Uri(
            "pack://application:,,,/AppFramework.Controls;component/Themes/Controls.xaml",
            UriKind.Absolute);

        var exists = app.Resources.MergedDictionaries
            .Any(d => d.Source == controlsUri);

        if (!exists)
        {
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = controlsUri
            });
        }
    }

    private string GetSettingsPath(string? userId)
    {
        var safeUser = string.IsNullOrWhiteSpace(userId) ? "default" : userId;
        safeUser = string.Join("_", safeUser.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_settingsFolder, safeUser, "theme.txt");
    }

    private static string GetDefaultSettingsFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "WPF.YS.Framework");
    }
}
