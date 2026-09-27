using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using AppFramework.Abstractions.Models.Navigation;
using SalesApp.Wpf.ViewModels;

namespace SalesApp.Wpf.Views;

public partial class ExplorerView : UserControl
{
    public ExplorerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        //PART_ExplorerHost.ItemActivated += OnItemActivated;

       
    }
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ExplorerViewModel vm && vm.Explorer is not null)
        {
            PART_Launcher.AttachExplorer(vm.Explorer);
            PART_History.AttachExplorer(vm.Explorer);

            // ✅ احترم الإعدادات
            var settings = vm.Explorer.Settings;
            PART_HistoryButton.Visibility = settings.ShowHistoryButton
                ? Visibility.Visible : Visibility.Collapsed;

            PART_Launcher.ItemActivated += (_, item) => vm.OnNavItemActivated(item);
            PART_Launcher.AddRequested += (_, _) => vm.OnAddRequested();
            PART_Launcher.SettingsRequested += (_, _) => vm.OnSettingsRequested();
            PART_Launcher.EditRequested += (_, item) => vm.OnEditRequested(item);
        }
    }

    private void OnHistoryClicked(object sender, RoutedEventArgs e)
    {
        ToggleHistory(PART_History.Visibility != Visibility.Visible);
    }

    private void ToggleHistory(bool show)
    {
        PART_History.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
            PART_History.Reload();
    }

    private void OnItemActivated(object? sender, NavItem item)
    {
        Debug.WriteLine($"[ExplorerView] Item activated: {item.Title}");
        if (DataContext is ExplorerViewModel vm)
            vm.OnNavItemActivated(item);
    }
}
