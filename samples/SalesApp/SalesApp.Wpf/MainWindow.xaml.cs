using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Menus;
using AppFramework.Controls.Screens;
using AppFramework.Controls.Theming;
using AppFramework.Core.Navigation;
using Microsoft.Extensions.DependencyInjection;
using SalesApp.Wpf.ViewModels;
using System;
using System.Windows;

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

        // اربط MenuHostControl
        PART_MenuHost.MenuManager = _menuManager;
        PART_MenuHost.ProviderRegistry = _services.GetRequiredService<MenuProviderRegistry>();

        DataContext = _shell;
        _shell.AttachServices(_services);

        // راقب تغيير ActiveScreenId (من Shell menu)
        _shell.PropertyChanged += OnShellPropertyChanged;

        // ✅ راقب كل تنقل (من أي مصدر)
        _navigation.Navigated += OnNavigated;

        // ✅ اربط Taskbar
        PART_Taskbar.AttachNavigation(_navigation);
        // ✅ اربط خدمة الثيم
        var themeService = _services.GetService<IThemeService>();
        PART_Taskbar.AttachThemeService(themeService);
        Loaded += OnLoaded;
    }

    // ==========================================================
    //  Lifecycle
    // ==========================================================

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // سجّل قائمة Shell
        _menuManager.RegisterScreen("Shell", _shell.BuildMenu);
        _menuManager.ActivateScreen("Shell", _shell);

        // افتح Explorer افتراضيًا
        _shell.OpenExplorerCommand.Execute(null);
    }

    protected override void OnClosed(EventArgs e)
    {
        _shell.PropertyChanged -= OnShellPropertyChanged;
        _navigation.Navigated -= OnNavigated;
        base.OnClosed(e);
    }

    // ==========================================================
    //  Navigation
    // ==========================================================

    private void OnNavigated(object? sender, NavigationContext context)
    {
        try
        {
            // 1) حدّث Explorer (تمييز الشاشة النشطة)
            var explorer = _services.GetService<INavigationExplorer>();
            explorer?.SetActiveScreen(context.TargetId);

            // 2) اربط PART_ScreenHost بالـ ViewModel الجديد
            BindScreenToHost(context.TargetId);

            // 3) حدّث ActiveScreenId في Shell (لشريط الحالة)
            //    ملاحظة: نستخدم backing مباشر لتجنّب الحلقة
            UpdateShellActiveScreenId(context.TargetId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] OnNavigated error: {ex.Message}");
        }
    }

    private bool _updatingShell;

    private void UpdateShellActiveScreenId(string screenId)
    {
        if (_shell.ActiveScreenId == screenId) return;

        _updatingShell = true;
        try
        {
            _shell.ActiveScreenId = screenId;
        }
        finally
        {
            _updatingShell = false;
        }
    }

    private SalesApp.Wpf.Views.ExplorerView? _explorerViewInstance;

    private void BindScreenToHost(string screenId)
    {
        if (_navigation is not NavigationService navService) return;

        var vm = navService.GetOpenScreen(screenId)?.ViewModel;
        if (vm is null) return;

        Dispatcher.Invoke(() =>
        {
            if (screenId == "Explorer")
            {
                // أنشئ مرة واحدة، أعد استخدامها
                _explorerViewInstance ??= new SalesApp.Wpf.Views.ExplorerView();

                PART_ScreenHost.Visibility = Visibility.Collapsed;
                PART_CustomHost.Visibility = Visibility.Visible;
                PART_CustomHost.Content = _explorerViewInstance;
                _explorerViewInstance.DataContext = vm;
            }
            else
            {
                PART_CustomHost.Visibility = Visibility.Collapsed;
                PART_CustomHost.Content = null;

                PART_ScreenHost.Visibility = Visibility.Visible;
                PART_ScreenHost.DataContext = vm;
            }
        });
    }

    // ==========================================================
    //  Shell menu
    // ==========================================================

    private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ShellViewModel.ActiveScreenId)) return;

        // تجاهل التحديثات التي أطلقناها نحن
        if (_updatingShell) return;

        var screenId = _shell.ActiveScreenId;
        if (string.IsNullOrEmpty(screenId)) return;

        // إن كانت الشاشة مفتوحة مسبقًا → فقط اربط
        if (_navigation is NavigationService navService)
        {
            var open = navService.GetOpenScreen(screenId);
            if (open is not null)
            {
                BindScreenToHost(screenId);
                return;
            }
        }

        // افتح الشاشة
        _ = OpenScreenAsync(screenId);
    }

    private async System.Threading.Tasks.Task OpenScreenAsync(string screenId)
    {
        try
        {
            // NavigationService.OpenScreenAsync سيُطلق Navigated
            // → OnNavigated سيتولى BindScreenToHost
            await _navigation.OpenScreenAsync(screenId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"فشل فتح الشاشة {screenId}:\n{ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
