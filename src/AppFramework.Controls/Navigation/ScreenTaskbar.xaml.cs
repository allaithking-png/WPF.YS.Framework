using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppFramework.Abstractions.Services;

namespace AppFramework.Controls.Navigation;

using AppFramework.Controls.Theming;

/// <summary>
/// شريط سفلي يعرض كل الشاشات المفتوحة.
/// </summary>
public partial class ScreenTaskbar : UserControl
{
    private INavigationService? _navigation;
    private IThemeService? _themeService;
    private bool _isUpdatingThemeCombo;
    public ObservableCollection<OpenScreenTabVm> Tabs { get; } = new();

    public ScreenTaskbar()
    {
        InitializeComponent();
        PART_Tabs.ItemsSource = Tabs;
      
        // املأ ComboBox
        PART_ThemeCombo.ItemsSource = Enum.GetValues(typeof(AppTheme));
        PART_ThemeCombo.SelectedIndex = 0;
    }
    public void AttachThemeService(IThemeService? themeService)
    {
        if (_themeService is not null)
            _themeService.ThemeChanged -= OnThemeServiceChanged;

        _themeService = themeService;

        if (_themeService is not null)
        {
            _themeService.ThemeChanged += OnThemeServiceChanged;

            _isUpdatingThemeCombo = true;
            try
            {
                PART_ThemeCombo.SelectedItem = _themeService.CurrentTheme;
            }
            finally
            {
                _isUpdatingThemeCombo = false;
            }
        }
    }

    private void OnThemeServiceChanged(object? sender, AppTheme theme)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnThemeServiceChanged(sender, theme));
            return;
        }

        _isUpdatingThemeCombo = true;
        try
        {
            PART_ThemeCombo.SelectedItem = theme;
        }
        finally
        {
            _isUpdatingThemeCombo = false;
        }
    }
    private void OnThemeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingThemeCombo) return;
        if (_themeService is null) return;
        if (PART_ThemeCombo.SelectedItem is not AppTheme theme) return;

        _themeService.ApplyTheme(theme);

        // احفظ الثيم
        try { _themeService.SaveCurrentTheme("demo-user"); }
        catch { }
    }
    public void AttachNavigation(INavigationService? navigation)
    {
        if (_navigation is not null)
        {
            _navigation.Navigated -= OnNavigated;
            _navigation.ScreenOpened -= OnScreenChanged;
            _navigation.ScreenClosed -= OnScreenChanged;
            _navigation.ActiveScreenChanged -= OnActiveChanged;
        }

        _navigation = navigation;

        if (_navigation is not null)
        {
            _navigation.Navigated += OnNavigated;
            _navigation.ScreenOpened += OnScreenChanged;
            _navigation.ScreenClosed += OnScreenChanged;
            _navigation.ActiveScreenChanged += OnActiveChanged;
        }

        Reload();
    }

    private void OnNavigated(object? sender, AppFramework.Abstractions.Models.Navigation.NavigationContext ctx)
        => Reload();

    private void OnScreenChanged(object? sender, OpenScreenInfo info) => Reload();

    private void OnActiveChanged(object? sender, string? screenId) => Reload();

    private void Reload()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(Reload);
            return;
        }

        Tabs.Clear();
        if (_navigation is null) return;

        var screens = _navigation.OpenScreens;
        foreach (var s in screens)
        {
            Tabs.Add(new OpenScreenTabVm
            {
                ScreenId = s.ScreenId,
                InstanceKey = s.InstanceKey,
                Title = s.Title,
                Icon = s.Icon ?? "•",
                IsActive = s.IsActive
            });
        }

        PART_Status.Text = $"{Tabs.Count} شاشة مفتوحة";
    }

    private void OnTabClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not OpenScreenTabVm tab) return;

        _ = _navigation?.ActivateScreenAsync(tab.ScreenId);
    }

    private void OnTabRightClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        if (fe.DataContext is not OpenScreenTabVm tab) return;

        var menu = new ContextMenu();

        menu.Items.Add(MakeItem("↗️ تفعيل", () =>
            _ = _navigation?.ActivateScreenAsync(tab.ScreenId)));

        menu.Items.Add(new Separator());

        menu.Items.Add(MakeItem("✖ إغلاق", () =>
            _ = _navigation?.CloseScreenAsync(tab.ScreenId)));

        menu.Items.Add(MakeItem("✖ إغلاق كل ما عداها", () =>
            _ = _navigation?.CloseAllExceptAsync(tab.ScreenId)));

        menu.Items.Add(new Separator());

        menu.Items.Add(MakeItem("✖ إغلاق الكل", () =>
            _ = _navigation?.CloseAllAsync()));

        menu.PlacementTarget = fe;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private static MenuItem MakeItem(string header, Action action)
    {
        var mi = new MenuItem { Header = header };
        mi.Click += (_, _) => action();
        return mi;
    }
}

public sealed class OpenScreenTabVm
{
    public string ScreenId { get; set; } = "";
    public string InstanceKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "•";
    public bool IsActive { get; set; }
}
