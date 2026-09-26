using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Services;
using AppFramework.Core.Navigation;
using AppFramework.Core.ViewTemplates;
using AppFramework.Controls.Menus;
using SalesApp.Wpf.ViewModels;

namespace SalesApp.Wpf;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly IServiceProvider _services;
    private readonly IViewTemplateHost _viewHost;
    private readonly INavigationService _navigation;

    public MainWindow(ShellViewModel shell, IServiceProvider services)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _viewHost = _services.GetRequiredService<IViewTemplateHost>();
        _navigation = _services.GetRequiredService<INavigationService>();

        InitializeComponent();

        // اربط MenuHostControl
        PART_MenuHost.MenuManager = _services.GetRequiredService<IMenuManager>();
        PART_MenuHost.ProviderRegistry = _services.GetRequiredService<MenuProviderRegistry>();

        DataContext = _shell;
        _shell.AttachServices(_services);

        _viewHost.ViewRendered += OnViewRendered;
        _shell.PropertyChanged += OnShellPropertyChanged;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // سجّل قائمة Shell
        var menu = _services.GetRequiredService<IMenuManager>();
        menu.RegisterScreen("Shell", _shell.BuildMenu);
        menu.ActivateScreen("Shell", _shell);

        // افتح Orders
        _shell.OpenOrdersCommand.Execute(null);
    }

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellViewModel.ActiveScreenId)) return;
        var screenId = _shell.ActiveScreenId;
        if (string.IsNullOrEmpty(screenId)) return;

        _ = OpenScreenInternalAsync(screenId);
    }

    private System.EventHandler<AppFramework.Abstractions.Models.ViewTemplates.ViewMode>? _currentModeHandler;

    private async System.Threading.Tasks.Task OpenScreenInternalAsync(string screenId)
    {
        try
        {
            await _navigation.OpenScreenAsync(screenId);

            var navService = _navigation as NavigationService;
            var vm = navService?.GetOpenScreen(screenId)?.ViewModel;
            if (vm is null) return;

            // ✅ افصل الاشتراك القديم
            DetachModeHandler();

            var defaultMode = AppFramework.Abstractions.Models.ViewTemplates.ViewMode.Grid;
            _viewHost.ShowInMode(vm, defaultMode, "MainContentRegion");

            // ✅ إذا كان الـ ViewModel يدعم IViewModeAware، اشترك في ModeChanged
            if (vm is IViewModeAware modeAware)
            {
                _currentModeHandler = (_, newMode) =>
                {
                    Dispatcher.Invoke(() =>
                        _viewHost.ShowInMode(vm, newMode, "MainContentRegion"));
                };
                modeAware.ModeChanged += _currentModeHandler;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل فتح الشاشة {screenId}:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DetachModeHandler()
    {
        if (_currentModeHandler is null) return;

        // نبحث عن الشاشة الحالية
        var navService = _navigation as NavigationService;
        var current = navService?.Current;
        if (current is not null)
        {
            var vm = navService?.GetOpenScreen(current.TargetId)?.ViewModel;
            if (vm is IViewModeAware modeAware)
                modeAware.ModeChanged -= _currentModeHandler;
        }

        _currentModeHandler = null;
    }

    private void OnViewRendered(object? sender, ViewRenderedEventArgs e)
    {
        Dispatcher.Invoke(() => PART_MainContent.Content = e.View);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewHost.ViewRendered -= OnViewRendered;
        _shell.PropertyChanged -= OnShellPropertyChanged;
        base.OnClosed(e);
    }
}
