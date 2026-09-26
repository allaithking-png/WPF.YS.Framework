using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Core.Screens;
using SalesApp.Wpf.Data;
namespace SalesApp.Wpf.ViewModels;

[Screen("Customers.List", "العملاء", Icon = "Users", Category = "المبيعات", DataSourceKey = "Customers")]
public sealed class CustomersViewModel : BaseListViewModel<CustomerDto>
{
    public override string ScreenId => "Customers.List";
    public override string ScreenTitle => "العملاء";
    public override string DataSourceKey => "Customers";

    public override IReadOnlyList<ViewMode> SupportedModes { get; } =
        new[] { ViewMode.Grid, ViewMode.Card, ViewMode.List };

    protected override void OnServicesAttached()
    {
        if (Data is not null)
            Reports?.RegisterReport("Customers.Report", SalesReportGenerators.CustomersReport(Data));
    }
}
