using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Models.Reports;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using AppFramework.Core.Screens;
using SalesApp.Wpf.Data;

namespace SalesApp.Wpf.ViewModels;

[Screen("Orders.List", "الطلبات", Icon = "Cart", Category = "المبيعات", DataSourceKey = "Orders")]
[ViewTemplate(ViewMode.Grid, typeof(Views.OrdersGridView), IsDefault = true, Title = "شبكة")]
[ViewTemplate(ViewMode.Kanban, typeof(Views.OrdersKanbanView), Title = "كانبان")]
public sealed class OrdersViewModel : BaseListViewModel<OrderDto>
{
    public override string ScreenId => "Orders.List";
    public override string ScreenTitle => "الطلبات";
    public override string DataSourceKey => "Orders";
    public override int PageSize => 5;
    public override IReadOnlyList<ViewMode> SupportedModes { get; } =
     new[] { ViewMode.Grid, ViewMode.Card, ViewMode.List, ViewMode.Kanban };
    protected override void OnServicesAttached()
    {
        // سجّل تقرير الطلبات تلقائيًا
        if (Data is not null)
            Reports?.RegisterReport("Orders.Report", SalesReportGenerators.OrdersReport(Data));
    }

    protected override void Add()
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
        SelectedItem = order;
        StatusMessage = $"{Items.Count} طلب";
        Notify?.Info("طلب جديد", $"تم إنشاء {order.Key}");
    }

    // أضف Kanban إلى قائمة العرض
    protected override List<AppFramework.Abstractions.Models.Menu.MenuItemDescriptor> BuildViewItems()
    {
        var items = base.BuildViewItems();
        items.Add(new AppFramework.Abstractions.Models.Menu.MenuItemDescriptor
        {
            Title = "كانبان",
            Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() => SetMode(ViewMode.Kanban))
        });
        return items;
    }
}
