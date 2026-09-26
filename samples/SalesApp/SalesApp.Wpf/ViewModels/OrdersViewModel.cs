using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.Reports;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using SalesApp.Wpf.Data;
using SalesApp.Wpf.Views;

namespace SalesApp.Wpf.ViewModels;

[Screen("Orders.List", "الطلبات", Icon = "Cart", Category = "المبيعات", DataSourceKey = "Orders")]
[ViewTemplate(ViewMode.Grid, typeof(OrdersGridView), IsDefault = true, Title = "شبكة")]
[ViewTemplate(ViewMode.Kanban, typeof(OrdersKanbanView), Title = "كانبان")]
public sealed partial class OrdersViewModel : ObservableObject, IAppAware, IScreenAware, IDataAware, IViewModeAware, IMenuAware
{
    private IServiceProvider? _services;
    private IDataService? _data;
    private IReportService? _reports;
    private INotificationService? _notify;
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

    public void AttachServices(IServiceProvider services)
    {
        _services = services;
        _data = services.GetRequiredService<IDataService>();
        _reports = services.GetRequiredService<IReportService>();
        _notify = services.GetRequiredService<INotificationService>();
        _viewHost = services.GetRequiredService<IViewTemplateHost>();

        // سجّل التقارير
        _reports.RegisterReport("Orders.Report", SalesReportGenerators.OrdersReport(_data));
        _reports.RegisterReport("Products.Report", SalesReportGenerators.ProductsReport(_data));
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
                if (item is OrderDto dto) Items.Add(dto);

            StatusMessage = $"{Items.Count} طلب";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
            _notify?.Error("خطأ", $"فشل تحميل الطلبات: {ex.Message}");
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

    // ==========================================================
    //  Menu
    // ==========================================================

    public MenuDefinition BuildMenu(MenuContext context) => new()
    {
        Host = context.PreferredHost,
        Items = new()
        {
            new MenuItemDescriptor
            {
                Title = "الطلبات",
                GroupName = "الطلبات",
                Order = 1,
                Children =
                {
                    new MenuItemDescriptor { Title = "تحديث", Command = RefreshCommand },
                    new MenuItemDescriptor { Title = "طلب جديد", Command = AddOrderCommand },
                    new MenuItemDescriptor { IsSeparator = true },
                    new MenuItemDescriptor { Title = "تصدير CSV", Command = ExportCsvCommand },
                    new MenuItemDescriptor { Title = "تصدير Excel", Command = ExportExcelCommand },
                    new MenuItemDescriptor { Title = "تصدير PDF", Command = ExportPdfCommand }
                }
            },
            new MenuItemDescriptor
            {
                Title = "العرض",
                GroupName = "العرض",
                Order = 2,
                Children =
                {
                    new MenuItemDescriptor { Title = "شبكة", Command = ShowGridCommand },
                    new MenuItemDescriptor { Title = "كانبان", Command = ShowKanbanCommand }
                }
            }
        }
    };

    // ==========================================================
    //  Commands
    // ==========================================================

    [RelayCommand]
    private void ShowGrid() => SetMode(ViewMode.Grid);

    [RelayCommand]
    private void ShowKanban() => SetMode(ViewMode.Kanban);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadInitialAsync();
        _notify?.Success("تحديث", "تم تحديث الطلبات بنجاح");
    }

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
        StatusMessage = $"أُضيف طلب: {order.Key}";
        _notify?.Info("طلب جديد", $"تم إنشاء {order.Key}");
    }

    [RelayCommand]
    private Task ExportCsvAsync() => ExportAsync(ReportExportFormat.Csv, "csv");

    [RelayCommand]
    private Task ExportExcelAsync() => ExportAsync(ReportExportFormat.Excel, "xlsx");

    [RelayCommand]
    private Task ExportPdfAsync() => ExportAsync(ReportExportFormat.Pdf, "pdf");

    private async Task ExportAsync(ReportExportFormat format, string ext)
    {
        if (_reports is null) return;

        try
        {
            var file = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"Orders-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}");

            await _reports.ExportAsync(
                new ReportRequest { ReportId = "Orders.Report" },
                format, file);

            _notify?.ShowWithActions(
                "تم التصدير",
                $"حُفظ التقرير في:\n{file}",
                new NotificationAction("فتح الملف", () =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file)
                    {
                        UseShellExecute = true
                    });
                }));
        }
        catch (Exception ex)
        {
            _notify?.Error("فشل التصدير", ex.Message);
        }
    }
}
