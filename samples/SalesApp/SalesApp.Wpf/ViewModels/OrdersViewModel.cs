using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SalesApp.Wpf.Data;
using SalesApp.Wpf.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SalesApp.Wpf.ViewModels;

[Screen("Orders.List", "الطلبات", Icon = "Cart", Category = "المبيعات", DataSourceKey = "Orders")]
[ViewTemplate(ViewMode.Grid, typeof(OrdersGridView), IsDefault = true, Title = "شبكة")]
[ViewTemplate(ViewMode.Kanban, typeof(OrdersKanbanView), Title = "كانبان")]
public sealed partial class OrdersViewModel : ObservableObject, IAppAware, IScreenAware, IDataAware, IViewModeAware
{
    private IServiceProvider? _services;
    private IDataService? _data;
    private IViewTemplateHost? _viewHost;

    public string ScreenId => "Orders.List";
    public string ScreenTitle => "الطلبات";
    public string? DataSourceKey => "Orders";

    public ObservableCollection<OrderDto> Items { get; } = new();

    public IReadOnlyList<ViewMode> SupportedModes { get; } = new[] { ViewMode.Grid, ViewMode.Kanban };

    [ObservableProperty] private ViewMode _currentMode = ViewMode.Grid;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "جاهز";

    public event EventHandler<ViewMode>? ModeChanged;

    public MenuDefinition BuildMenu(MenuContext context) => new()
    {
        Host = context.PreferredHost,
        Items = new()
    {
        new MenuItemDescriptor
        {
            Title = "الطلبات",
            GroupName = "الطلبات",
            Children =
            {
                new MenuItemDescriptor { Title = "تحديث", Command = RefreshCommand },
                new MenuItemDescriptor { Title = "جديد", Command = AddOrderCommand }
            }
        },
        new MenuItemDescriptor
        {
            Title = "العرض",
            GroupName = "العرض",
            Children =
            {
                new MenuItemDescriptor { Title = "شبكة", Command = ShowGridCommand },
                new MenuItemDescriptor { Title = "كانبان", Command = ShowKanbanCommand }
            }
        }
    }
    };

    public void AttachServices(IServiceProvider services)
    {
        _services = services;
        _data = services.GetRequiredService<IDataService>();
        _viewHost = services.GetRequiredService<IViewTemplateHost>();
    }

    public async Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
    {
        await LoadInitialAsync(ct);
    }

    public Task OnDeactivatedAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> OnClosingAsync(CancellationToken ct = default) => Task.FromResult(true);

    public async Task LoadInitialAsync(CancellationToken ct = default)
    {
        if (_data is null) return;

        IsBusy = true;
        StatusMessage = "جارٍ التحميل...";

        try
        {
            Items.Clear();
            var result = await _data.LoadBatchAsync(new DataRequest("Orders", Page: 1, PageSize: 50), ct);

            foreach (var item in result.Items)
            {
                if (item is OrderDto dto) Items.Add(dto);
            }

            StatusMessage = $"{Items.Count} طلب";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    public Task LoadMoreAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void SetMode(ViewMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        ModeChanged?.Invoke(this, mode);
    }

    [RelayCommand]
    private void ShowGrid() => SetMode(ViewMode.Grid);

    [RelayCommand]
    private void ShowKanban() => SetMode(ViewMode.Kanban);

    [RelayCommand]
    private async Task RefreshAsync() => await LoadInitialAsync();

    [RelayCommand]
    private void AddOrder()
    {
        var order = new OrderDto
        {
            Key = $"ORD-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            CustomerName = "عميل جديد",
            Amount = 0,
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };
        Items.Insert(0, order);
        StatusMessage = $"أُضيف طلب جديد: {order.Key}";
    }
}
