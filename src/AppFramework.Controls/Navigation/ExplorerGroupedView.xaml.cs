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
/// عرض أيقونات سطح المكتب في مجموعات.
/// </summary>
public partial class ExplorerGroupedView : UserControl
{
    private INavigationExplorer? _explorer;
    private readonly ILogger<ExplorerGroupedView>? _logger;

    public ObservableCollection<ExplorerGroup> Groups { get; } = new();

    public event EventHandler<NavItem>? ItemActivated;

    public ExplorerGroupedView()
    {
        InitializeComponent();
        PART_Groups.ItemsSource = Groups;
        PART_SearchBox.TextChanged += OnSearchTextChanged;
    }

    public ExplorerGroupedView(ILogger<ExplorerGroupedView> logger) : this()
    {
        _logger = logger;
    }

    // ==========================================================
    //  Attach
    // ==========================================================

   
    public void AttachExplorer(INavigationExplorer? explorer)
    {
        if (_explorer is not null)
        {
            _explorer.TreeChanged -= OnTreeChanged;
            _explorer.ActiveScreenChanged -= OnActiveScreenChanged;   // ← جديد
        }

        _explorer = explorer;

        if (_explorer is not null)
        {
            _explorer.TreeChanged += OnTreeChanged;
            _explorer.ActiveScreenChanged += OnActiveScreenChanged;   // ← جديد
        }

        Rebuild();
    }

    // ==========================================================
    //  Building
    // ==========================================================

    private void Rebuild()
    {
        Groups.Clear();
        if (_explorer is null) return;

        var root = _explorer.BuildRoot();
        _logger?.LogDebug("Building groups from {Count} root items", root.Count);

        foreach (var group in root)
        {
            if (group.Kind != NavItemKind.Folder) continue;

            var allItems = Flatten(group)
                .Where(i => i.Kind != NavItemKind.Folder)
                .ToList();

            if (allItems.Count == 0) continue;

            Groups.Add(new ExplorerGroup
            {
                Title = group.Title,
                Items = new ObservableCollection<NavItem>(allItems)
            });
        }
    }

    private static IEnumerable<NavItem> Flatten(NavItem folder)
    {
        foreach (var child in folder.Children)
        {
            if (child.Kind == NavItemKind.Folder)
            {
                foreach (var sub in Flatten(child)) yield return sub;
            }
            else
            {
                yield return child;
            }
        }
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
    //  Icon Events
    // ==========================================================

    private static NavItem? GetItem(object sender)
        => (sender as FrameworkElement)?.DataContext as NavItem;

    private void OnIconMouseDown(object sender, MouseButtonEventArgs e)
    {
        // نقر مزدوج → فتح
        if (e.ClickCount == 2)
        {
            var item = GetItem(sender);
            if (item is null) return;

            if (item.Kind == NavItemKind.Folder) return;

            ItemActivated?.Invoke(this, item);
        }
    }

    private void OnIconOpenClicked(object sender, RoutedEventArgs e)
    {
        var item = GetItemFromMenu(sender);
        if (item is null) return;

        ItemActivated?.Invoke(this, item);
    }

    private void OnIconTogglePinClicked(object sender, RoutedEventArgs e)
    {
        var item = GetItemFromMenu(sender);
        if (item is null) return;
        _explorer?.TogglePin(item.Id);
    }

    private void OnIconToggleFavoriteClicked(object sender, RoutedEventArgs e)
    {
        var item = GetItemFromMenu(sender);
        if (item is null) return;
        _explorer?.ToggleFavorite(item.Id);
    }

    private void OnIconHideClicked(object sender, RoutedEventArgs e)
    {
        var item = GetItemFromMenu(sender);
        if (item is null) return;
        _explorer?.Hide(item.Id);
    }

    /// <summary>الحصول على NavItem من ContextMenu.</summary>
    private static NavItem? GetItemFromMenu(object sender)
    {
        if (sender is not MenuItem menuItem) return null;
        if (menuItem.Parent is not ContextMenu cm) return null;
        if (cm.PlacementTarget is not FrameworkElement fe) return null;
        return fe.DataContext as NavItem;
    }

    // ==========================================================
    //  Search
    // ==========================================================

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        PART_ClearButton.Visibility = string.IsNullOrWhiteSpace(PART_SearchBox.Text)
            ? Visibility.Collapsed : Visibility.Visible;

        var query = PART_SearchBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(query))
        {
            Rebuild();
            return;
        }

        if (_explorer is null) return;

        var results = _explorer.Search(query);

        Groups.Clear();
        Groups.Add(new ExplorerGroup
        {
            Title = $"🔍 نتائج البحث ({results.Count})",
            Items = new ObservableCollection<NavItem>(results)
        });
    }

    private void OnClearSearchClicked(object sender, RoutedEventArgs e)
    {
        PART_SearchBox.Text = "";
        PART_SearchBox.Focus();
    }
    

    private void OnActiveScreenChanged(object? sender, string? screenId)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnActiveScreenChanged(sender, screenId));
            return;
        }
        Rebuild();
    }
}

public sealed class ExplorerGroup
{
    public string Title { get; set; } = "";
    public ObservableCollection<NavItem> Items { get; set; } = new();
}
