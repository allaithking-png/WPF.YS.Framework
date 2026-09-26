using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Data;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Models.Navigation;
using AppFramework.Abstractions.Models.Reports;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

    /// <summary>كل العناصر (المصدر).</summary>
    public ObservableCollection<TDto> Items { get; } = new();

    /// <summary>العناصر المعروضة (بعد الفلترة).</summary>
    public ObservableCollection<TDto> FilteredItems { get; } = new();

    [ObservableProperty] private ViewMode _currentMode = ViewMode.Grid;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "جاهز";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private TDto? _selectedItem;
    // ==========================================================
    //  Paging
    // ==========================================================

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount = 0;

    /// <summary>هل يمكن الانتقال للصفحة التالية؟</summary>
    public bool CanGoNext => CurrentPage < TotalPages;

    /// <summary>هل يمكن الانتقال للصفحة السابقة؟</summary>
    public bool CanGoPrevious => CurrentPage > 1;

    /// <summary>نص حالة الصفحة (مثال: "صفحة 1 من 5 - 100 عنصر").</summary>
    public string PageStatus => $"صفحة {CurrentPage} من {TotalPages} • {TotalCount} عنصر";


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
        CurrentPage = 1;
        await LoadPageAsync(ct);
    }
    public async Task LoadInitialAsync2(CancellationToken ct = default)
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

            // ✅ أعِد بناء FilteredItems
            ApplyFilter();

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
    partial void OnCurrentPageChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(PageStatus));
    }

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageStatus));
    }

    partial void OnTotalCountChanged(int value)
    {
        OnPropertyChanged(nameof(PageStatus));
    }

    /// <summary>يُستدعى عند تغيير SearchText.</summary>
    partial void OnSearchTextChanged(string value)
    {
        // عند البحث، ارجع للصفحة 1
        if (CurrentPage != 1)
        {
            CurrentPage = 1;
        }
        ApplyFilter();
    }

    /// <summary>يطبّق الفلترة على Items → FilteredItems.</summary>
    protected virtual void ApplyFilter()
    {
        FilteredItems.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var item in Items) FilteredItems.Add(item);
            return;
        }

        var query = SearchText.Trim();
        var searchFields = SearchFields.Count > 0
            ? SearchFields
            : InferSearchFields();

        foreach (var item in Items)
        {
            if (MatchesSearch(item, query, searchFields))
                FilteredItems.Add(item);
        }

        StatusMessage = $"{FilteredItems.Count} من {Items.Count} عنصر";
    }

    private bool MatchesSearch(TDto item, string query, IReadOnlyList<string> fields)
    {
        if (item is null) return false;

        foreach (var fieldName in fields)
        {
            var prop = item.GetType().GetProperty(fieldName);
            if (prop is null) continue;

            var val = prop.GetValue(item)?.ToString();
            if (!string.IsNullOrEmpty(val) &&
                val.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<string> InferSearchFields2()
    {
        // استخدم أول حقل نصي متاح
        var props = typeof(TDto).GetProperties();
        var fields = new List<string>();

        if (props.Any(p => p.Name == "Key")) fields.Add("Key");
        if (props.Any(p => p.Name == "Name")) fields.Add("Name");
        if (props.Any(p => p.Name == "Title")) fields.Add("Title");
        if (props.Any(p => p.Name == "City")) fields.Add("City");

        return fields.Count > 0 ? fields : props.Take(2).Select(p => p.Name).ToList();
    }
    private IReadOnlyList<string> InferSearchFields()
    {
        var props = typeof(TDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // 1) أولوية: خاصيات معنونة بـ [Searchable]
        var annotated = props
            .Select(p => new
            {
                Prop = p,
                Attr = p.GetCustomAttribute<AppFramework.Abstractions.Attributes.SearchableAttribute>(inherit: true)
            })
            .Where(x => x.Attr is not null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Prop.Name)
            .ToList();

        if (annotated.Count > 0)
            return annotated;

        // 2) fallback: حقول نصية شائعة
        var fallback = new List<string>();
        if (props.Any(p => p.Name == "Key")) fallback.Add("Key");
        if (props.Any(p => p.Name == "Name")) fallback.Add("Name");
        if (props.Any(p => p.Name == "Title")) fallback.Add("Title");
        if (props.Any(p => p.Name == "City")) fallback.Add("City");

        if (fallback.Count > 0) return fallback;

        // 3) آخر حل: كل الحقول النصية
        var stringProps = props
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name)
            .ToList();

        return stringProps.Count > 0 ? stringProps : new List<string> { props.FirstOrDefault()?.Name ?? "" };
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
    private async Task GoFirstPageAsync()
    {
        if (CurrentPage == 1) return;
        CurrentPage = 1;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task GoPreviousPageAsync()
    {
        if (!CanGoPrevious) return;
        CurrentPage--;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task GoNextPageAsync()
    {
        if (!CanGoNext) return;
        CurrentPage++;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task GoLastPageAsync()
    {
        if (CurrentPage == TotalPages) return;
        CurrentPage = TotalPages;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
        await LoadPageAsync();
    }
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
        if (Data is null) return;

        // أنشئ كيانًا جديدًا
        var newItem = Activator.CreateInstance<TDto>();
        if (newItem is null) return;

        // عيّن مفتاحًا مؤقتًا
        var keyProp = typeof(TDto).GetProperty("Key") ?? typeof(TDto).GetProperty("Id");
        if (keyProp?.CanWrite == true && keyProp.PropertyType == typeof(string))
        {
            keyProp.SetValue(newItem, $"NEW-{Guid.NewGuid().ToString("N")[..6].ToUpper()}");
        }

        // افتح نافذة الحوار
        var dialog = new AppFramework.Controls.Screens.GenericFormDialog(
            newItem, $"إضافة {ScreenTitle}");

        // عيّن Owner
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null && owner != dialog)
            dialog.Owner = owner;

        var result = dialog.ShowDialog();

        if (result == true && dialog.Saved)
        {
            Items.Insert(0, newItem);
            SelectedItem = newItem;
            StatusMessage = $"{Items.Count} عنصر";

            // حفظ في مصدر البيانات
            _ = SaveNewItemAsync(newItem);
            // ✅ أعِد بناء FilteredItems
            ApplyFilter();
            Notify?.Success("إضافة", $"تم إضافة {GetDisplayName(newItem)}");
        }
    }

    private async System.Threading.Tasks.Task SaveNewItemAsync(TDto item)
    {
        if (Data is null) return;
        try
        {
            await Data.SaveItemAsync(DataSourceKey, item!);
        }
        catch (Exception ex)
        {
            Notify?.Error("فشل الحفظ", ex.Message);
        }
    }

    [RelayCommand]
    protected virtual void Edit()
    {
        if (SelectedItem is null) return;

        // انسخ الكيان لتجنّب التعديل المباشر قبل الحفظ
        var clone = CloneDto(SelectedItem);
        if (clone is null) return;

        var dialog = new AppFramework.Controls.Screens.GenericFormDialog(
            clone, $"تعديل {GetDisplayName(SelectedItem)}");

        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null && owner != dialog)
            dialog.Owner = owner;

        var result = dialog.ShowDialog();

        if (result == true && dialog.Saved)
        {
            // انسخ القيم من الـ clone إلى العنصر الأصلي
            CopyValues(clone, SelectedItem);

            // أعد تحميل القائمة للحصول على أحدث عرض
            var index = Items.IndexOf(SelectedItem);
            if (index >= 0)
            {
                var temp = SelectedItem;
                Items.RemoveAt(index);
                Items.Insert(index, temp);
                SelectedItem = temp;
            }

            // حفظ
            _ = SaveExistingItemAsync(SelectedItem);

            Notify?.Success("تعديل", $"تم تحديث {GetDisplayName(SelectedItem)}");
        }
    }

    private async System.Threading.Tasks.Task SaveExistingItemAsync(TDto item)
    {
        if (Data is null) return;
        try
        {
            await Data.SaveItemAsync(DataSourceKey, item!);
        }
        catch (Exception ex)
        {
            Notify?.Error("فشل الحفظ", ex.Message);
        }
    }

    // ==========================================================
    //  Clone / Copy helpers
    // ==========================================================

    private static TDto? CloneDto(TDto source)
    {
        if (source is null) return null;

        var clone = Activator.CreateInstance<TDto>();
        if (clone is null) return null;

        CopyValues(source, clone);
        return clone;
    }

    private static void CopyValues(object from, object to)
    {
        foreach (var p in from.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!p.CanRead || !p.CanWrite) continue;
            try { p.SetValue(to, p.GetValue(from)); }
            catch { /* تجاهل */ }
        }
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
            // ✅ أعِد بناء FilteredItems
            ApplyFilter();
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


    /// <summary>يحمّل الصفحة الحالية.</summary>
    protected virtual async Task LoadPageAsync(CancellationToken ct = default)
    {
        if (Data is null) return;

        IsBusy = true;
        StatusMessage = "جارٍ التحميل...";

        try
        {
            var result = await Data.LoadBatchAsync(
                new DataRequest(DataSourceKey, Page: CurrentPage, PageSize: PageSize), ct);

            Items.Clear();
            foreach (var item in result.Items.OfType<TDto>())
                Items.Add(item);

            // حدّث حالة الصفحات
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling((double)result.TotalCount / PageSize));

            ApplyFilter();
            StatusMessage = PageStatus;
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ: {ex.Message}";
            Notify?.Error("خطأ", $"فشل تحميل {ScreenTitle}: {ex.Message}");
        }
        finally { IsBusy = false; }
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
