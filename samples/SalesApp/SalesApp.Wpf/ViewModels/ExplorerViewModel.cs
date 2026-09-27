using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;

namespace SalesApp.Wpf.ViewModels;

/// <summary>
/// شاشة مستكشف التنقل.
/// </summary>
[Screen("Explorer", "مستكشف التنقل", Icon = "Tree", Category = "النظام")]
[ViewTemplate(ViewMode.Grid, typeof(Views.ExplorerView), IsDefault = true, Title = "شجرة")]
public sealed partial class ExplorerViewModel : ObservableObject, IAppAware, IScreenAware, IMenuAware
{
    private IServiceProvider? _services;
    private INavigationExplorer? _explorer;
    private INavigationService? _navigation;

    public string ScreenId => "Explorer";
    public string ScreenTitle => "مستكشف التنقل";

    [ObservableProperty] private string _statusMessage = "جاهز";

    public INavigationExplorer? Explorer => _explorer;

    /// <summary>حدث عند النقر على عنصر في الشجرة (يُعالَج في الـ View).</summary>
    public event EventHandler<NavItem>? ItemActivated;

    public void AttachServices(IServiceProvider services)
    {
        _services = services;
        _explorer = services.GetService<INavigationExplorer>();
        _navigation = services.GetService<INavigationService>();
    }

    public async Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
    {
        if (_explorer is null) return;
        await _explorer.LoadAsync("demo-user", ct);
        StatusMessage = "شجرة التنقل جاهزة";
       
    }

    public Task OnDeactivatedAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> OnClosingAsync(CancellationToken ct = default) => Task.FromResult(true);

    public MenuDefinition BuildMenu(MenuContext context) => new()
    {
        Host = context.PreferredHost,
        Items = new()
        {
            new MenuItemDescriptor
            {
                Title = "المستكشف",
                GroupName = "المستكشف",
                Order = 1,
                Children =
                {
                    new MenuItemDescriptor { Title = "تحديث", Command = new CommunityToolkit.Mvvm.Input.RelayCommand(Refresh) }
                }
            }
        }
    };
    public void OnAddRequested()
    {
        if (_explorer is null) return;

        // ✅ اجمع كل العناصر المتاحة
        var allItems = _explorer.GetAllItems().ToList();

        var folders = CollectFolders(_explorer.GetPersonalTree());

        // الشاشات المتاحة (من النظام، غير المضافة بعد)
        var screens = allItems.Where(i => i.Kind == NavItemKind.Screen).ToList();
        var reports = allItems.Where(i => i.Kind == NavItemKind.Report).ToList();

        var dialog = new AppFramework.Controls.Navigation.AddItemDialog(folders, screens, reports);
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null) dialog.Owner = owner;

        if (dialog.ShowDialog() == true && dialog.Result is { } r)
        {
            try
            {
                switch (r.Kind)
                {
                    case "Screen":
                        _explorer.AddScreen(r.Title, r.Target, "🖥️", r.FolderId);
                        break;
                    case "Report":
                        _explorer.AddReport(r.Title, r.Target, "📊", r.FolderId);
                        break;
                    case "Url":
                        _explorer.AddUrl(r.Title, r.Target, "🔗", r.FolderId);
                        break;
                    case "ExternalFile":
                        _explorer.AddExternalFile(r.Target, r.Title, r.FolderId);
                        break;
                    case "ExternalFolder":
                        _explorer.AddExternalFolder(r.Target, r.Title, r.FolderId);
                        break;
                    case "LocalFolder":
                        _explorer.AddLocalFolder(r.Title, "📂", r.FolderId);
                        break;
                }
                StatusMessage = $"أُضيف: {r.Title}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ: {ex.Message}";
            }
        }
    }
    public void OnSettingsRequested()
    {
        if (_explorer is null) return;

        var dialog = new AppFramework.Controls.Navigation.LauncherSettingsDialog(_explorer.Settings);
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null) dialog.Owner = owner;

        if (dialog.ShowDialog() == true && dialog.Result is { } settings)
        {
            _explorer.UpdateSettings(settings);
            StatusMessage = "تم تحديث الإعدادات";
        }
    }

    public void OnEditRequested(NavItem item)
    {
        if (_explorer is null || item is null) return;

        var dialog = new AppFramework.Controls.Navigation.EditItemDialog(item);
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null) dialog.Owner = owner;

        if (dialog.ShowDialog() == true && dialog.Result is { } edited)
        {
            _explorer.UpdateItem(edited.Id, i =>
            {
                i.Title = edited.Title;
                i.Icon = edited.Icon;
                i.CustomIconSize = edited.CustomIconSize;
                i.CustomBackground = edited.CustomBackground;
            });
            StatusMessage = "تم التعديل";
        }
    }

    private static System.Collections.Generic.List<NavItem> CollectFolders(
        System.Collections.Generic.IEnumerable<NavItem> items,
        System.Collections.Generic.List<NavItem>? acc = null)
    {
        acc ??= new System.Collections.Generic.List<NavItem>();
        foreach (var item in items)
        {
            if (item.Kind == NavItemKind.Folder)
            {
                acc.Add(item);
                CollectFolders(item.Children, acc);
            }
        }
        return acc;
    }
    /// <summary>يُستدعى من الـ View عند نقر عنصر.</summary>
    public void OnNavItemActivated(NavItem item)
    {
        if (item is null) return;

        System.Diagnostics.Debug.WriteLine($"[Explorer] NavItem activated: {item.Title} ({item.Kind}) Target={item.Target}");

        ItemActivated?.Invoke(this, item);

       
        // ✅ سجّل الشاشة النشطة
        if (item.Kind == NavItemKind.Screen && !string.IsNullOrEmpty(item.Target))
        {
            _explorer?.SetActiveScreen(item.Target);
            _ = _navigation?.OpenScreenAsync(item.Target);
            StatusMessage = $"فتح: {item.Title}";
        }
        else if (item.Kind == NavItemKind.Report && !string.IsNullOrEmpty(item.Target))
        {
            _explorer?.SetActiveScreen(item.Target);
            StatusMessage = $"تقرير: {item.Title}";
        }
        else
        {
            // ملفات / مجلدات / روابط → افتحها مباشرة
            _explorer?.RequestOpen(item);
            StatusMessage = $"فتح: {item.Title}";
        }
    }

    private void Refresh()
    {
        StatusMessage = "تحديث...";
    }
}
