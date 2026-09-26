using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.Reports;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;

namespace AppFramework.Core.Screens;

/// <summary>
/// أساس عام لكل شاشات القوائم.
/// يوفّر تلقائيًا: تحميل، حفظ، حذف، تحديث، تقارير، إشعارات، قوائم، وأوضاع عرض.
/// </summary>
/// <typeparam name="TDto">نوع الكيان (DTO).</typeparam>
public abstract partial class BaseListViewModel<TDto> : ObservableObject,
    IAppAware, IScreenAware, IDataAware, IViewModeAware, IMenuAware, IReportAware
    where TDto : class
{
    protected IServiceProvider? Services { get; private set; }
    protected IDataService? Data { get; private set; }
    protected IReportService? Reports { get; private set; }
    protected INotificationService? Notify { get; private set; }
    protected IViewTemplateHost? ViewHost { get; private set; }

    // ==========================================================
    //  Abstract / Virtual
    // ==========================================================

    /// <summary>معرّف الشاشة.</summary>
    public abstract string ScreenId { get; }

    /// <summary>عنوان الشاشة.</summary>
    public abstract string ScreenTitle { get; }

    /// <summary>مفتاح مصدر البيانات.</summary>
    public abstract string DataSourceKey { get; }

    /// <summary>معرّف التقرير المرتبط (افتراضيًا: {DataSourceKey}.Report).</summary>
    public virtual string ReportId => $"{DataSourceKey}.Report";

    /// <summary>طرق العرض المدعومة.</summary>
    public virtual IReadOnlyList<ViewMode> SupportedModes { get; } =
        new[] { ViewMode.Grid, ViewMode.Card };

    /// <summary>حجم الصفحة.</summary>
    public virtual int PageSize => 100;

    /// <summary>حقول البحث.</summary>
    public virtual IReadOnlyList<string> SearchFields => Array.Empty<string>();

    // ==========================================================
    //  State
    // ==========================================================

    public ObservableCollection<TDto> Items { get; } = new();

    [ObservableProperty] private ViewMode _currentMode = ViewMode.Grid;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "جاهز";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private TDto? _selectedItem;

    public event EventHandler<ViewMode>? ModeChanged;

    // ==========================================================
    //  IAppAware
    // ==========================================================

    public virtual void AttachServices(IServiceProvider services)
    {
        Services = services;
        Data = services.GetService<IDataService>();
        Reports = services.GetService<IReportService>();
        Notify = services.GetService<INotificationService>();
        ViewHost = services.GetService<IViewTemplateHost>();

        OnServicesAttached();
    }

    /// <summary>نقطة تمديد بعد ربط الخدمات.</summary>
    protected virtual void OnServicesAttached() { }

    // ==========================================================
    //  IScreenAware
    // ==========================================================

    public virtual async Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
    {
        await LoadInitialAsync(ct);
    }

    public virtual Task OnDeactivatedAsync(CancellationToken ct = default) => Task.CompletedTask;
    public virtual Task<bool> OnClosingAsync(CancellationToken ct = default) => Task.FromResult(true);

    // ==========================================================
    //  IDataAware
    // ==========================================================

    public async Task LoadInitialAsync(CancellationToken ct = default)
    {
        if (Data is null) return;

        IsBusy = true;
        StatusMessage = "جارٍ التحميل...";

        try
        {
            var result = await Data.LoadBatchAsync(
                new DataRequest(DataSourceKey, Page: 1, PageSize: PageSize), ct);

            Items.Clear();
            foreach (var item in result.Items.OfType<TDto>())
                Items.Add(item);

            StatusMessage = $"{Items.Count} عنصر";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
            Notify?.Error("خطأ", $"فشل تحميل {ScreenTitle}: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    public Task LoadMoreAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;

    // ==========================================================
    //  IViewModeAware
    // ==========================================================

    
    public void SetMode(ViewMode mode)
    {
        if (CurrentMode == mode) return;
        if (!SupportedModes.Contains(mode)) return;

        CurrentMode = mode;
        ModeChanged?.Invoke(this, mode);   // ← ✅ موجود
    }

    // ==========================================================
    //  IMenuAware — قائمة تلقائية
    // ==========================================================

    public virtual MenuDefinition BuildMenu(MenuContext context) => new()
    {
        Host = context.PreferredHost,
        Items = new()
        {
            // مجموعة "إجراءات"
            new MenuItemDescriptor
            {
                Title = ScreenTitle,
                GroupName = ScreenTitle,
                Order = 1,
                Children = BuildActionItems()
            },

            // مجموعة "العرض"
            new MenuItemDescriptor
            {
                Title = "العرض",
                GroupName = "العرض",
                Order = 2,
                Children = BuildViewItems()
            },

            // مجموعة "التقارير"
            new MenuItemDescriptor
            {
                Title = "التقارير",
                GroupName = "التقارير",
                Order = 3,
                Children = BuildReportItems()
            }
        }
    };

    protected virtual List<MenuItemDescriptor> BuildActionItems() => new()
    {
        new MenuItemDescriptor { Title = "تحديث", Command = RefreshCommand },
        new MenuItemDescriptor { Title = "جديد", Command = AddCommand },
        new MenuItemDescriptor { Title = "تعديل", Command = EditCommand, IsEnabled = SelectedItem is not null },
        new MenuItemDescriptor { Title = "حذف", Command = DeleteCommand, IsEnabled = SelectedItem is not null }
    };

    protected virtual List<MenuItemDescriptor> BuildViewItems()
    {
        var items = new List<MenuItemDescriptor>();

        if (SupportedModes.Contains(ViewMode.Grid))
            items.Add(new MenuItemDescriptor { Title = "شبكة", Command = ShowGridCommand });

        if (SupportedModes.Contains(ViewMode.Card))
            items.Add(new MenuItemDescriptor { Title = "بطاقات", Command = ShowCardCommand });

        if (SupportedModes.Contains(ViewMode.List))
            items.Add(new MenuItemDescriptor { Title = "قائمة", Command = ShowListCommand });

        return items;
    }

    protected virtual List<MenuItemDescriptor> BuildReportItems() => new()
    {
        new MenuItemDescriptor { Title = "تصدير CSV", Command = ExportCsvCommand },
        new MenuItemDescriptor { Title = "تصدير Excel", Command = ExportExcelCommand },
        new MenuItemDescriptor { Title = "تصدير PDF", Command = ExportPdfCommand }
    };

    // ==========================================================
    //  IReportAware
    // ==========================================================

    public virtual IReadOnlyList<string> LinkedReportIds => new[] { ReportId };

    // ==========================================================
    //  Commands
    // ==========================================================

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadInitialAsync();
        Notify?.Success("تحديث", $"تم تحديث {ScreenTitle} بنجاح");
    }

    [RelayCommand]
    private void ShowGrid() => SetMode(ViewMode.Grid);

    [RelayCommand]
    private void ShowCard() => SetMode(ViewMode.Card);

    [RelayCommand]
    private void ShowList() => SetMode(ViewMode.List);

    [RelayCommand]
    protected virtual void Add()
    {
        // يمكن تجاوزه في الفئات المشتقة
        Notify?.Info("جديد", "إضافة عنصر جديد (غير مفعّل هنا)");
    }

    [RelayCommand]
    protected virtual void Edit()
    {
        if (SelectedItem is null) return;
        Notify?.Info("تعديل", $"تعديل: {GetDisplayName(SelectedItem)}");
    }

    [RelayCommand]
    protected virtual async Task DeleteAsync()
    {
        if (SelectedItem is null || Data is null) return;

        var name = GetDisplayName(SelectedItem);
        var key = ExtractKey(SelectedItem);
        if (string.IsNullOrEmpty(key)) return;

        try
        {
            await Data.DeleteAsync(DataSourceKey, key);
            Items.Remove(SelectedItem);
            StatusMessage = $"{Items.Count} عنصر";
            Notify?.Success("حذف", $"تم حذف: {name}");
        }
        catch (Exception ex)
        {
            Notify?.Error("فشل الحذف", ex.Message);
        }
    }

    [RelayCommand]
    private Task ExportCsvAsync() => ExportAsync(ReportExportFormat.Csv, "csv");

    [RelayCommand]
    private Task ExportExcelAsync() => ExportAsync(ReportExportFormat.Excel, "xlsx");

    [RelayCommand]
    private Task ExportPdfAsync() => ExportAsync(ReportExportFormat.Pdf, "pdf");

    protected async Task ExportAsync(ReportExportFormat format, string ext)
    {
        if (Reports is null) return;

        try
        {
            Notify?.Info("جارٍ التصدير", $"توليد تقرير {ScreenTitle} ({format})...");

            var file = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"{DataSourceKey}-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}");

            await Reports.ExportAsync(
                new ReportRequest { ReportId = ReportId },
                format, file);

            Notify?.ShowWithActions(
                "تم التصدير بنجاح",
                $"{ScreenTitle}: {Items.Count} عنصر\n{file}",
                new NotificationAction("فتح الملف", () =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file)
                    {
                        UseShellExecute = true
                    });
                }),
                new NotificationAction("فتح المجلد", () =>
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file}\"");
                }));
        }
        catch (Exception ex)
        {
            Notify?.Error("فشل التصدير", ex.Message);
        }
    }

    // ==========================================================
    //  Helpers
    // ==========================================================

    /// <summary>اسم العرض للعنصر (يمكن تجاوزه).</summary>
    protected virtual string GetDisplayName(TDto item)
    {
        var prop = item.GetType().GetProperty("Name")
                ?? item.GetType().GetProperty("Title")
                ?? item.GetType().GetProperty("Key");
        return prop?.GetValue(item)?.ToString() ?? item.GetType().Name;
    }

    /// <summary>استخراج المفتاح من الكيان.</summary>
    protected static string? ExtractKey(TDto item)
    {
        var prop = item.GetType().GetProperty("Key")
                ?? item.GetType().GetProperty("Id")
                ?? item.GetType().GetProperty("ItemKey");
        return prop?.GetValue(item)?.ToString();
    }

    // عند تغيير SelectedItem، أعد بناء القائمة (لتفعيل أزرار Edit/Delete)
    partial void OnSelectedItemChanged(TDto? value)
    {
        // إعادة بناء القائمة
        var menuManager = Services?.GetService<IMenuManager>();
        menuManager?.Refresh();
    }
}
