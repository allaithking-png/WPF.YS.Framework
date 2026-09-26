using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Menus;
using AppFramework.Core.Navigation;
using SalesApp.Wpf.ViewModels;

namespace SalesApp.Wpf;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly IServiceProvider _services;
    private readonly INavigationService _navigation;
    private readonly IMenuManager _menuManager;

    public MainWindow(ShellViewModel shell, IServiceProvider services)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        _navigation = _services.GetRequiredService<INavigationService>();
        _menuManager = _services.GetRequiredService<IMenuManager>();

        InitializeComponent();

        // ربط MenuHostControl
        PART_MenuHost.MenuManager = _menuManager;
        PART_MenuHost.ProviderRegistry = _services.GetRequiredService<MenuProviderRegistry>();

        // DataContext للـ Shell
        DataContext = _shell;
        _shell.AttachServices(_services);

        // راقب تغيير الشاشة النشطة
        _shell.PropertyChanged += OnShellPropertyChanged;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // سجّل قائمة Shell
        _menuManager.RegisterScreen("Shell", _shell.BuildMenu);
        _menuManager.ActivateScreen("Shell", _shell);

        // افتح شاشة Orders افتراضيًا
        _shell.OpenOrdersCommand.Execute(null);
    }

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellViewModel.ActiveScreenId)) return;
        var screenId = _shell.ActiveScreenId;
        if (string.IsNullOrEmpty(screenId)) return;

        _ = OpenScreenAsync(screenId);
    }

    private async System.Threading.Tasks.Task OpenScreenAsync(string screenId)
    {
        try
        {
            await _navigation.OpenScreenAsync(screenId);

            var navService = _navigation as NavigationService;
            var vm = navService?.GetOpenScreen(screenId)?.ViewModel;
            if (vm is null)
            {
                MessageBox.Show($"لم يتم العثور على ViewModel للشاشة {screenId}", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ScreenViewHost يستمع لـ IViewModeAware.ModeChanged تلقائيًا
            PART_ScreenHost.DataContext = vm;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل فتح الشاشة {screenId}:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void OnModeChanged(object? sender, AppFramework.Abstractions.Models.ViewTemplates.ViewMode mode)
    {
        Dispatcher.Invoke(() => PART_ScreenHost.ViewMode = mode);
    }

    protected override void OnClosed(EventArgs e)
    {
        _shell.PropertyChanged -= OnShellPropertyChanged;
        base.OnClosed(e);
    }
}
