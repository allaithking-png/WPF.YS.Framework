using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

/// <summary>
/// Shell لـ Explorer — يغلّف ExplorerTreeControl مع رأس.
/// </summary>
public partial class ExplorerHost : UserControl
{
    private INavigationExplorer? _explorer;

    public ExplorerHost()
    {
        InitializeComponent();
        PART_Tree.ItemActivated += OnItemActivated;
    }

    public event EventHandler<NavItem>? ItemActivated;

    /// <summary>اربط خدمة Explorer.</summary>
    public void AttachExplorer(INavigationExplorer? explorer)
    {
        _explorer = explorer;
        PART_Tree.AttachExplorer(explorer);
    }

    public void Refresh()
    {
        if (_explorer is not null)
            PART_Tree.AttachExplorer(_explorer);
    }

    private void OnItemActivated(object? sender, NavItem item)
        => ItemActivated?.Invoke(this, item);

    private void OnRefreshClicked(object sender, RoutedEventArgs e) => Refresh();
}
