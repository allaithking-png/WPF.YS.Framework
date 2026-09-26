using System.Collections.Generic;
using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Core.Screens;
using SalesApp.Wpf.Data;

namespace SalesApp.Wpf.ViewModels;

[Screen("Products.List", "المنتجات", Icon = "Box", Category = "المبيعات", DataSourceKey = "Products")]
[ViewTemplate(ViewMode.Grid, typeof(Views.ProductsView), IsDefault = true, Title = "شبكة")]
public sealed class ProductsViewModel : BaseListViewModel<ProductDto>
{
    public override string ScreenId => "Products.List";
    public override string ScreenTitle => "المنتجات";
    public override string DataSourceKey => "Products";
    public override IReadOnlyList<ViewMode> SupportedModes { get; } =
    new[] { ViewMode.Grid, ViewMode.Card };

    protected override void OnServicesAttached()
    {
        if (Data is not null)
            Reports?.RegisterReport("Products.Report", SalesReportGenerators.ProductsReport(Data));
    }
}
