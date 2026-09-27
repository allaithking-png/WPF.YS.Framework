using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

public partial class HistoryPanel : UserControl
{
    private INavigationExplorer? _explorer;
    private string _filter = "all";

    public ObservableCollection<HistoryVm> Items { get; } = new();

    public event EventHandler? CloseRequested;

    public HistoryPanel()
    {
        InitializeComponent();
        PART_History.ItemsSource = Items;
    }

    // ==========================================================
    //  Attach
    // ==========================================================

    public void AttachExplorer(INavigationExplorer? explorer)
    {
        if (_explorer is not null)
            _explorer.HistoryChanged -= OnHistoryChanged;

        _explorer = explorer;

        if (_explorer is not null)
            _explorer.HistoryChanged += OnHistoryChanged;

        Reload();
    }

    private void OnHistoryChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(Reload);
            return;
        }
        Reload();
    }

    // ==========================================================
    //  Loading
    // ==========================================================

    public void Reload()
    {
        Items.Clear();
        if (_explorer is null) return;

        IEnumerable<HistoryEntry> source = _filter switch
        {
            "unseen" => _explorer.GetUnseenHistory(100),
            "created" => _explorer.GetHistory(200).Where(h => h.Kind == HistoryKind.Created),
            "updated" => _explorer.GetHistory(200).Where(h => h.Kind == HistoryKind.Updated
                                                          || h.Kind == HistoryKind.Moved),
            _ => _explorer.GetHistory(100)
        };

        foreach (var entry in source)
        {
            Items.Add(new HistoryVm
            {
                Id = entry.Id,
                Icon = entry.GetIcon(),
                Title = entry.Title,
                Details = entry.Details,
                KindLabel = entry.GetKindLabel(),
                RelativeTime = entry.GetRelativeTime(),
                IsUnseen = _explorer.GetUnseenHistory(100).Any(h => h.Id == entry.Id)
            });
        }

        var unseen = _explorer.UnseenHistoryCount;
        PART_Status.Text = $"{Items.Count} حدث • {unseen} غير مشاهد";
    }

    // ==========================================================
    //  Events
    // ==========================================================

    private void OnFilterClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _filter = btn.Tag as string ?? "all";
        Reload();
    }

    private void OnMarkAllSeenClicked(object sender, RoutedEventArgs e)
    {
        _explorer?.MarkAllHistorySeen();
        Reload();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
        => CloseRequested?.Invoke(this, EventArgs.Empty);
}

public sealed class HistoryVm
{
    public string Id { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Details { get; set; }
    public string KindLabel { get; set; } = "";
    public string RelativeTime { get; set; } = "";
    public bool IsUnseen { get; set; }

    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);
}