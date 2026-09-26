using AppFramework.Abstractions.Attributes;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SalesApp.Wpf.Data;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace SalesApp.Wpf.ViewModels;

[Screen("Products.List", "المنتجات", Icon = "Box", Category = "المبيعات", DataSourceKey = "Products")]
[ViewTemplate(ViewMode.Grid, typeof(Views.ProductsView), IsDefault = true, Title = "شبكة")]
public sealed partial class ProductsViewModel : ObservableObject, IAppAware, IScreenAware, IDataAware
{
    private IServiceProvider? _services;
    private IDataService? _data;

    public string ScreenId => "Products.List";
    public string ScreenTitle => "المنتجات";
    public string? DataSourceKey => "Products";

    public ObservableCollection<ProductDto> Items { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "جاهز";

    public MenuDefinition BuildMenu(MenuContext context) => new()
    {
        Host = context.PreferredHost,
        Items = new()
        {
            new MenuItemDescriptor
            {
                Title = "المنتجات",
                GroupName = "المنتجات",
                Children =
                {
                    new MenuItemDescriptor { Title = "تحديث", CommandKey = "Products.Refresh" }
                }
            }
        }
    };

    public void AttachServices(IServiceProvider services)
    {
        _services = services;
        _data = services.GetRequiredService<IDataService>();
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
        try
        {
            Items.Clear();
            var result = await _data.LoadBatchAsync(new DataRequest("Products", Page: 1, PageSize: 50), ct);
            foreach (var item in result.Items)
                if (item is ProductDto dto) Items.Add(dto);
            StatusMessage = $"{Items.Count} منتج";
        }
        catch (Exception ex) { StatusMessage = $"خطأ: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    public Task LoadMoreAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
}
