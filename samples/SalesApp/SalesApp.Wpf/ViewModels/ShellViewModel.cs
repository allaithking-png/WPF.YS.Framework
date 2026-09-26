using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Theming;

namespace SalesApp.Wpf.ViewModels;

/// <summary>
/// Shell الرئيسي — يدير التبويبات + القائمة العلوية + تبديل الثيمات.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject, IAppAware, IMenuAware
{
    private IServiceProvider? _services;

    [ObservableProperty]
    private string _title = "SalesApp — WPF.YS.Framework Demo";

    [ObservableProperty]
    private AppTheme _currentTheme = AppTheme.Light;

    /// <summary>الشاشة النشطة حاليًا (الاسم).</summary>
    [ObservableProperty]
    private string _activeScreenId = "";

    public ObservableCollection<AppTheme> AvailableThemes { get; } = new()
    {
        AppTheme.Light,
        AppTheme.Dark,
        AppTheme.Corporate
    };

    public ShellViewModel()
    {
        // استرجع الثيم الحالي
        var themeService = AppFramework.Core.DependencyInjection.AppServices.TryGet<IThemeService>();
        if (themeService is not null)
        {
            CurrentTheme = themeService.CurrentTheme;
            themeService.ThemeChanged += (_, theme) =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() => CurrentTheme = theme);
            };
        }
    }

    public void AttachServices(IServiceProvider services) => _services = services;

    public MenuDefinition BuildMenu(MenuContext context) => new()
    {
        Host = context.PreferredHost,
        Items = new()
        {
            new MenuItemDescriptor
            {
                Title = "التنقل",
                GroupName = "التنقل",
                Order = 1,
                Children =
                {
                    new MenuItemDescriptor { Title = "الطلبات", Command = OpenOrdersCommand },
                    new MenuItemDescriptor { Title = "المنتجات", Command = OpenProductsCommand }
                }
            },
            new MenuItemDescriptor
            {
                Title = "الثيمات",
                GroupName = "المظهر",
                Order = 2,
                Children = new()
                {
                    new MenuItemDescriptor { Title = "فاتح", Command = new RelayCommand(() => ChangeTheme(AppTheme.Light)) },
                    new MenuItemDescriptor { Title = "داكن", Command = new RelayCommand(() => ChangeTheme(AppTheme.Dark)) },
                    new MenuItemDescriptor { Title = "مؤسسي", Command = new RelayCommand(() => ChangeTheme(AppTheme.Corporate)) }
                }
            },
            new MenuItemDescriptor
            {
                Title = "خروج",
                GroupName = "التطبيق",
                Order = 3,
                Command = new RelayCommand(() => System.Windows.Application.Current?.Shutdown())
            }
        }
    };

    [RelayCommand]
    private void OpenOrders() => ActiveScreenId = "Orders.List";

    [RelayCommand]
    private void OpenProducts() => ActiveScreenId = "Products.List";

    private void ChangeTheme(AppTheme theme)
    {
        var themeService = _services?.GetService<IThemeService>();
        themeService?.ApplyTheme(theme);
        CurrentTheme = theme;
    }
}