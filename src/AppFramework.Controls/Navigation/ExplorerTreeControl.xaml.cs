using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

/// <summary>
/// شجرة Explorer لعرض شاشات/تقارير/ملفات النظام بشكل هرمي.
/// </summary>
public partial class ExplorerTreeControl : UserControl
{
    private INavigationExplorer? _explorer;
    private readonly ILogger<ExplorerTreeControl>? _logger;

    public ObservableCollection<NavTreeItem> Root { get; } = new();

    public ExplorerTreeControl()
    {
        InitializeComponent();

        PART_Tree.ItemsSource = Root;

        // البحث (debounce)
        PART_SearchBox.TextChanged += OnSearchTextChanged;
    }

    public ExplorerTreeControl(ILogger<ExplorerTreeControl> logger) : this()
    {
        _logger = logger;
    }

    // ==========================================================
    //  Public API
    // ==========================================================

    /// <summary>اربط الخدمة (يُستدعى من Shell).</summary>
    public void AttachExplorer(INavigationExplorer? explorer)
    {
        if (_explorer is not null)
            _explorer.TreeChanged -= OnTreeChanged;

        _explorer = explorer;

        if (_explorer is not null)
            _explorer.TreeChanged += OnTreeChanged;

        Rebuild();
    }

    /// <summary>يُطلق عند طلب فتح عنصر.</summary>
    public event EventHandler<NavItem>? ItemActivated;

    // ==========================================================
    //  Tree Building
    // ==========================================================

    private void Rebuild()
    {
        Root.Clear();
        if (_explorer is null) return;

        var items = _explorer.BuildRoot();
        foreach (var item in items)
            Root.Add(NavTreeItem.FromNavItem(item));
    }

    private void OnTreeChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(Rebuild);
            return;
        }
        Rebuild();
    }

    // ==========================================================
    //  Interaction
    // ==========================================================

    private void OnTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var selected = PART_Tree.SelectedItem as NavTreeItem;
        if (selected is null) return;
        if (selected.Kind == NavItemKind.Folder) return;   // لا تفتح المجلدات

        Activate(selected.NavItem);
    }

    private void OnTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // (اختياري) معاينة سريعة
    }

    private void Activate(NavItem item)
    {
        if (item.Kind == NavItemKind.Screen || item.Kind == NavItemKind.Report)
        {
            _explorer?.RecordOpen(item.Id);
            ItemActivated?.Invoke(this, item);
        }
        else if (item.Kind == NavItemKind.Url && !string.IsNullOrEmpty(item.Target))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Target)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to open URL: {Url}", item.Target);
            }
        }
        else if (item.Kind == NavItemKind.ExternalFile && !string.IsNullOrEmpty(item.Target))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(item.Target)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to open file: {Path}", item.Target);
            }
        }
        else if (item.Kind == NavItemKind.ExternalFolder && !string.IsNullOrEmpty(item.Target))
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", item.Target);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to open folder: {Path}", item.Target);
            }
        }
    }

    // ==========================================================
    //  Context Menu
    // ==========================================================

    private NavTreeItem? SelectedTreeItem => PART_Tree.SelectedItem as NavTreeItem;

    private void OnOpenClicked(object sender, RoutedEventArgs e)
    {
        var s = SelectedTreeItem;
        if (s is not null && s.Kind != NavItemKind.Folder)
            Activate(s.NavItem);
    }

    private void OnTogglePinClicked(object sender, RoutedEventArgs e)
    {
        var s = SelectedTreeItem;
        if (s is null || _explorer is null) return;
        _explorer.TogglePin(s.NavItem.Id);
    }

    private void OnToggleFavoriteClicked(object sender, RoutedEventArgs e)
    {
        var s = SelectedTreeItem;
        if (s is null || _explorer is null) return;
        _explorer.ToggleFavorite(s.NavItem.Id);
    }

    private void OnHideClicked(object sender, RoutedEventArgs e)
    {
        var s = SelectedTreeItem;
        if (s is null || _explorer is null) return;
        _explorer.Hide(s.NavItem.Id);
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e) => Rebuild();

    // ==========================================================
    //  Search
    // ==========================================================

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        PART_ClearButton.Visibility = string.IsNullOrWhiteSpace(PART_SearchBox.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;

        var query = PART_SearchBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            Rebuild();
            return;
        }

        if (_explorer is null) return;

        var results = _explorer.Search(query);

        Root.Clear();
        var folder = new NavTreeItem(
            new NavItem
            {
                Id = "__search__",
                Title = $"نتائج البحث ({results.Count})",
                Kind = NavItemKind.Folder,
                Icon = "🔍"
            },
            isExpanded: true);

        foreach (var item in results)
            folder.Children.Add(NavTreeItem.FromNavItem(item));

        Root.Add(folder);
    }

    private void OnClearSearchClicked(object sender, RoutedEventArgs e)
    {
        PART_SearchBox.Text = "";
        PART_SearchBox.Focus();
    }
}

/// <summary>عنصر شجرة (يغلّف NavItem ويدعم WPF TreeView).</summary>
public sealed class NavTreeItem : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isExpanded;

    public NavItem NavItem { get; }
    public ObservableCollection<NavTreeItem> Children { get; } = new();

    public string Id => NavItem.Id;
    public string Title => NavItem.Title;
    public string? Icon => NavItem.Icon ?? DefaultIconForKind(NavItem.Kind);
    public NavItemKind Kind => NavItem.Kind;
    public bool IsFavorite => NavItem.IsFavorite;
    public bool IsPinned => NavItem.IsPinned;
    public int UsageCount => NavItem.UsageCount;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public NavTreeItem(NavItem item, bool isExpanded = false)
    {
        NavItem = item;
        _isExpanded = isExpanded;
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public static NavTreeItem FromNavItem(NavItem item)
    {
        var node = new NavTreeItem(item);
        foreach (var child in item.Children)
            node.Children.Add(FromNavItem(child));
        return node;
    }

    private static string DefaultIconForKind(NavItemKind kind) => kind switch
    {
        NavItemKind.Screen => "🖥️",
        NavItemKind.Report => "📊",
        NavItemKind.Folder => "📁",
        NavItemKind.ExternalFile => "📄",
        NavItemKind.ExternalFolder => "📁",
        NavItemKind.Url => "🔗",
        _ => "•"
    };
}
