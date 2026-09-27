using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.ViewTemplates;
using AppFramework.Abstractions.Services;
using AppFramework.Controls.Theming;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace AppFramework.Controls.Screens;

/// <summary>
/// Shell عام لأي شاشة قائمة.
/// </summary>
public partial class ScreenViewHost : UserControl
{
    private IViewModeAware? _currentModeAware;
    private INotifyPropertyChanged? _currentNotifyVm;
    private IThemeService? _themeService;
    public ScreenViewHost()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _themeService = AppFramework.Core.DependencyInjection.AppServices
                .TryGet<IThemeService>();

            if (_themeService is not null)
                _themeService.ThemeChanged += OnThemeChanged;
        }
        catch { }
    }
    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        Dispatcher.Invoke(() =>
        {
            // ✅ أعِد رسم المحتوى ليطبّق الثيم الجديد
            RenderContent();
        });
        //Dispatcher.Invoke(() =>
        //{
        //    if (PART_ContentHost.Content is DataGrid dg)
        //    {
        //        // ✅ أعِد ربط ItemsSource
        //        var items = dg.ItemsSource;
        //        dg.ItemsSource = null;
        //        dg.ItemsSource = items;

        //        // أو: أعِد تطبيق Style
        //        var rowStyle = dg.RowStyle;
        //        dg.RowStyle = null;
        //        dg.RowStyle = rowStyle;
        //    }
        //});
    }

    // ==========================================================
    //  Dependency Properties
    // ==========================================================

    public static readonly DependencyProperty ViewModeProperty =
        DependencyProperty.Register(
            nameof(ViewMode),
            typeof(ViewMode),
            typeof(ScreenViewHost),
            new PropertyMetadata(ViewMode.Grid, OnViewModeChanged));

    public ViewMode ViewMode
    {
        get => (ViewMode)GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public static readonly DependencyProperty ShowToolbarProperty =
        DependencyProperty.Register(
            nameof(ShowToolbar),
            typeof(bool),
            typeof(ScreenViewHost),
            new PropertyMetadata(true, OnShowToolbarChanged));

    public bool ShowToolbar
    {
        get => (bool)GetValue(ShowToolbarProperty);
        set => SetValue(ShowToolbarProperty, value);
    }

    private static void OnShowToolbarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScreenViewHost host && host.PART_ToolbarBorder is not null)
        {
            host.PART_ToolbarBorder.Visibility = e.NewValue is true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    // ==========================================================
    //  Lifecycle
    // ==========================================================

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachVmListener();
        DetachModeAware();
        if (e.NewValue is null) return;

        // ShowToolbar
        PART_ToolbarBorder.Visibility = ShowToolbar ? Visibility.Visible : Visibility.Collapsed;

        // Title
        var titleProp = e.NewValue.GetType().GetProperty("ScreenTitle");
        PART_Title.Text = titleProp?.GetValue(e.NewValue)?.ToString() ?? "";

        // CurrentMode
        var modeProp = e.NewValue.GetType().GetProperty("CurrentMode");
        if (modeProp?.GetValue(e.NewValue) is ViewMode mode)
            ViewMode = mode;

        // IViewModeAware
        if (e.NewValue is IViewModeAware modeAware)
        {
            _currentModeAware = modeAware;
            modeAware.ModeChanged += OnModeAwareChanged;
        }
        // ✅ اربط شريط البحث
        var searchBinding = new Binding("SearchText")
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Delay = 300   // debounce
        };
        PART_SearchBox.SetBinding(TextBox.TextProperty, searchBinding);

        // أظهر زر المسح عند وجود نص
        PART_SearchBox.TextChanged += (_, _) =>
        {
            PART_ClearSearchButton.Visibility = string.IsNullOrWhiteSpace(PART_SearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        };
        // ✅ حدّث الـ Pagination
        UpdatePaginationVisibility();

        // ✅ حدّث الأدوات
        //UpdateToolbarVisibility();
        // Toolbar
        BuildToolbar(e.NewValue);

        // Content
        RenderContent();
    }
    private void DetachVmListener()
    {
        if (_currentNotifyVm is not null)
        {
            _currentNotifyVm.PropertyChanged -= OnVmPropertyChanged;
            _currentNotifyVm = null;
        }
    }

    private int _lastTotalPages = -1;

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "TotalPages" && e.PropertyName != "TotalCount") return;

        var totalPagesProp = DataContext?.GetType().GetProperty("TotalPages");
        var totalPages = (int?)totalPagesProp?.GetValue(DataContext) ?? 1;

        if (totalPages == _lastTotalPages) return;   // ✅ لا تغيير → لا تُحدّث
        _lastTotalPages = totalPages;

        UpdatePaginationVisibility();
    }

    private void UpdatePaginationVisibility()
    {
        if (PART_PaginationBar is null) return;

        // اقرأ TotalPages من الـ ViewModel
        var totalPagesProp = DataContext?.GetType().GetProperty("TotalPages");
        var totalPages = totalPagesProp?.GetValue(DataContext) as int? ?? 1;

        PART_PaginationBar.Visibility = totalPages > 1
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
    private void OnClearSearchClicked(object sender, RoutedEventArgs e)
    {
        PART_SearchBox.Text = "";
        PART_ClearSearchButton.Visibility = Visibility.Collapsed;
    }
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachModeAware();
        DetachVmListener();   // ✅ جديد
        if (_themeService is not null)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
            _themeService = null;
        }

    }
    //private void UpdateSearchVisibility()
    //{
    //    if (PART_SearchBar is null) return;

    //    // إن كان ViewModel يدعم البحث
    //    var hasSearch = DataContext?.GetType().GetProperty("SearchText") is not null;
    //    var supportedModes = DataContext?.GetType().GetProperty("SupportedModes")?.GetValue(DataContext);

    //    // للـ BaseListViewModel → نُظهر البحث دائمًا
    //    // للـ شاشات أخرى → نُخفيه
    //    var isListVm = DataContext?.GetType().Name.Contains("ViewModel") == true;

    //    PART_SearchBar.Visibility = isListVm
    //        ? Visibility.Visible
    //        : Visibility.Collapsed;
    //}

    private void DetachModeAware()
    {
        if (_currentModeAware is not null)
        {
            _currentModeAware.ModeChanged -= OnModeAwareChanged;
            _currentModeAware = null;
        }
    }

    private void OnModeAwareChanged(object? sender, ViewMode mode)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnModeAwareChanged(sender, mode));
            return;
        }
        ViewMode = mode;
    }

    private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScreenViewHost host)
            host.RenderContent();
    }

    // ==========================================================
    //  Toolbar Builder
    // ==========================================================

    private void BuildToolbar(object viewModel)
    {
        var vmType = viewModel.GetType();

        // Actions
        var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal };
        AddCommandButton(actionsPanel, viewModel, "AddCommand", "➕", "جديد");
        AddCommandButton(actionsPanel, viewModel, "EditCommand", "✏️", "تعديل");
        AddCommandButton(actionsPanel, viewModel, "DeleteCommand", "🗑️", "حذف");
        AddCommandButton(actionsPanel, viewModel, "RefreshCommand", "🔄", "تحديث");
        PART_ActionsHost.Content = actionsPanel;

        // View Modes (dynamic)
        var viewModesPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var supportedModesProp = vmType.GetProperty("SupportedModes");
        if (supportedModesProp?.GetValue(viewModel) is IEnumerable<ViewMode> supportedModes)
        {
            foreach (var mode in supportedModes)
            {
                switch (mode)
                {
                    case ViewMode.Grid:
                        AddCommandButton(viewModesPanel, viewModel, "ShowGridCommand", "📊", "شبكة");
                        break;
                    case ViewMode.Card:
                        AddCommandButton(viewModesPanel, viewModel, "ShowCardCommand", "🗂️", "بطاقات");
                        break;
                    case ViewMode.List:
                        AddCommandButton(viewModesPanel, viewModel, "ShowListCommand", "📋", "قائمة");
                        break;
                    case ViewMode.Kanban:
                        // جرّب ShowKanbanCommand أولًا
                        if (GetCommand(viewModel, "ShowKanbanCommand") is not null)
                            AddCommandButton(viewModesPanel, viewModel, "ShowKanbanCommand", "📌", "كانبان");
                        break;
                }
            }
        }

        PART_ViewModesHost.Content = viewModesPanel;

        // Reports
        var reportsPanel = new StackPanel { Orientation = Orientation.Horizontal };
        AddCommandButton(reportsPanel, viewModel, "ExportCsvCommand", "📄", "CSV");
        AddCommandButton(reportsPanel, viewModel, "ExportExcelCommand", "📗", "Excel");
        AddCommandButton(reportsPanel, viewModel, "ExportPdfCommand", "📕", "PDF");
        PART_ReportsHost.Content = reportsPanel;
    }

    private static void AddCommandButton(Panel parent, object viewModel,
        string commandName, string content, string? tooltip)
    {
        var command = GetCommand(viewModel, commandName);
        if (command is null) return;

        parent.Children.Add(new Button
        {
            Content = content,
            Command = command,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(0, 0, 4, 0),
            MinWidth = 36,
            ToolTip = tooltip
        });
    }

    private static ICommand? GetCommand(object viewModel, string commandName)
    {
        var prop = viewModel.GetType().GetProperty(commandName);
        return prop?.GetValue(viewModel) as ICommand;
    }

    // ==========================================================
    //  Content
    // ==========================================================

    private void RenderContent()
    {
        if (DataContext is null) return;

        // 1) للـ Kanban والتخصيصات: استخدم ViewTemplate
        if (ViewMode == ViewMode.Kanban
            || ViewMode == ViewMode.Timeline
            || ViewMode == ViewMode.Calendar)
        {
            var registry = GetViewTemplateRegistry();
            var template = registry?.GetByMode(DataContext.GetType(), ViewMode);

            if (template?.ViewType is not null)
            {
                try
                {
                    var view = (FrameworkElement)Activator.CreateInstance(template.ViewType)!;
                    view.DataContext = DataContext;
                    PART_ContentHost.Content = view;
                    System.Diagnostics.Debug.WriteLine(
                        $"[ScreenViewHost] Rendered {template.ViewType.Name} via ViewTemplate");
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[ScreenViewHost] Failed to create {template.ViewType.Name}: {ex.Message}");
                }
            }
        }

        // 2) Fallback: Grid / Card / List
        FrameworkElement fallback = ViewMode switch
        {
            ViewMode.Card => new GenericCardView(),
            ViewMode.List => new GenericListView(),
            _ => CreateGridView()
        };

        fallback.DataContext = DataContext;
        PART_ContentHost.Content = fallback;

        System.Diagnostics.Debug.WriteLine($"[ScreenViewHost] Rendered {fallback.GetType().Name}");
    }

    private IViewTemplateRegistry? GetViewTemplateRegistry()
    {
        try
        {
            return AppFramework.Core.DependencyInjection.AppServices
                .TryGet<IViewTemplateRegistry>();
        }
        catch
        {
            return null;
        }
    }
    private ViewTemplateDescriptor? GetViewTemplateFor(Type vmType, ViewMode mode)
    {
        var registry = AppFramework.Core.DependencyInjection.AppServices
            .TryGet<AppFramework.Abstractions.Services.IViewTemplateRegistry>();
        return registry?.GetByMode(vmType, mode)
            ?? registry?.GetDefault(vmType);
    }

    private FrameworkElement CreateGridView()
    {
        var dataGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            RowHeight = 28
        };

        if (TryFindResource("App.SurfaceBrush") is Brush surface)
            dataGrid.AlternatingRowBackground = surface;
        if (TryFindResource("App.BorderBrush") is Brush border)
            dataGrid.HorizontalGridLinesBrush = border;

        var selectedBinding = new Binding("SelectedItem")
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        dataGrid.SetBinding(DataGrid.SelectedItemProperty, selectedBinding);
        dataGrid.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("FilteredItems"));

        var itemsProp = DataContext.GetType().GetProperty("Items");
        if (itemsProp?.GetValue(DataContext) is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is null) continue;
                foreach (var p in item.GetType().GetProperties())
                {
                    if (!p.CanRead) continue;
                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = p.Name,
                        Binding = new Binding(p.Name),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
                    });
                }
                break;
            }
        }

        dataGrid.Loaded += (_, _) =>
        {
            if (dataGrid.Columns.Count > 0) return;
            if (dataGrid.Items.Count == 0) return;
            var first = dataGrid.Items[0];
            if (first is null) return;
            foreach (var p in first.GetType().GetProperties())
            {
                if (!p.CanRead) continue;
                dataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = p.Name,
                    Binding = new Binding(p.Name),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
                });
            }
        };

        return dataGrid;
    }
}
