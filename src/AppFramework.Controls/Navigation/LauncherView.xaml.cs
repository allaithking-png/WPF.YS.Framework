using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Contracts;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

/// <summary>
/// سطح مكتب Android-style.
/// </summary>
public partial class LauncherView : UserControl
{
    private INavigationExplorer? _explorer;
    private readonly ILogger<LauncherView>? _logger;
    private LauncherSettings _settings = new();
    private Dictionary<string, int> _unseenCounts = new();
    private string? _selectedItemId;
    private NavItem? _draggedItem;

    public ObservableCollection<LauncherGroupVm> Groups { get; } = new();

    public event EventHandler<NavItem>? ItemActivated;
    public event EventHandler? AddRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler<NavItem>? EditRequested;

    public LauncherView()
    {
        InitializeComponent();
        PART_SearchBox.TextChanged += OnSearchTextChanged;
        MouseDown += (_, e) =>
        {
            if (e.OriginalSource is LauncherIcon) return;
            _selectedItemId = null;
            RenderGroups();
        };
    }

    public LauncherView(ILogger<LauncherView> logger) : this()
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
            _explorer.SettingsChanged -= OnSettingsChanged;
        }

        _explorer = explorer;

        if (_explorer is not null)
        {
            _explorer.TreeChanged += OnTreeChanged;
            _explorer.SettingsChanged += OnSettingsChanged;
            _settings = _explorer.Settings;
        }

        Rebuild();
    }

    private void OnSettingsChanged(object? sender, LauncherSettings settings)
    {
        _settings = settings;
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(Rebuild);
            return;
        }
        Rebuild();
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
    //  Rebuild
    // ==========================================================

    private void Rebuild()
    {
        Groups.Clear();
        if (_explorer is null) return;

        _unseenCounts = (_explorer as AppFramework.Core.Navigation.NavigationExplorerService)?
    .GetUnseenCountPerItem() ?? new Dictionary<string, int>();

        _settings = _explorer.Settings;
        var root = _explorer.BuildRoot();

        foreach (var group in root)
        {
            if (group.Kind != NavItemKind.Folder) continue;

            var all = Flatten(group).Where(i => i.Kind != NavItemKind.Folder
                                             || i.Kind == NavItemKind.Folder).ToList();
            // نبقي المجلدات
            var items = new List<NavItem>();
            CollectItems(group, items);

            if (items.Count == 0) continue;

            Groups.Add(new LauncherGroupVm
            {
                Title = group.Title,
                Items = items
            });
        }

        PART_Status.Text = $"{Groups.Count} مجموعة • {Groups.Sum(g => g.Items.Count)} عنصر";
        RenderGroups();
    }

    private static void CollectItems(NavItem node, List<NavItem> result)
    {
        foreach (var child in node.Children)
        {
            if (child.Kind == NavItemKind.Folder)
            {
                // في كل الحالات: فكّ المجلدات النظامية فقط
                // المجلدات الشخصية (التي أنشأها المستخدم) تُعرض كأيقونات
                if (IsSystemFolder(node))
                {
                    CollectItems(child, result);
                }
                else
                {
                    result.Add(child);   // مجلد المستخدم → أيقونة
                }
            }
            else
            {
                result.Add(child);
            }
        }
    }

    private static bool IsSystemFolder(NavItem node)
    {
        // المجموعات التي بناها BuildRoot (لها معرّفات خاصة بـ __)
        return node.Id.StartsWith("__") ||
               node.Id.StartsWith("__cat__");
    }
    private static IEnumerable<NavItem> Flatten(NavItem folder)
    {
        foreach (var child in folder.Children)
        {
            yield return child;

            if (child.Kind == NavItemKind.Folder)
            {
                foreach (var sub in Flatten(child))
                    yield return sub;
            }
        }
    }

    // ==========================================================
    //  Rendering
    // ==========================================================

    private void RenderGroups()
    {
        PART_Groups.Items.Clear();

        foreach (var group in Groups)
        {
            var groupPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };

            // رأس المجموعة
            var header = new TextBlock
            {
                Text = group.Title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(4, 0, 0, 6)
            };
            header.SetResourceReference(TextBlock.ForegroundProperty, "App.PrimaryBrush");
            groupPanel.Children.Add(header);

            // خط فاصل
            var separator = new Border
            {
                Height = 1,
                Margin = new Thickness(0, 0, 0, 10)
            };
            separator.SetResourceReference(Border.BackgroundProperty, "App.BorderBrush");
            groupPanel.Children.Add(separator);

            // WrapPanel
            var wrap = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                AllowDrop = true
            };
            wrap.DragOver += OnWrapDragOver;
            wrap.Drop += OnWrapDrop;

            foreach (var item in group.Items)
            {
                var icon = new LauncherIcon();
                var unseen = _unseenCounts.GetValueOrDefault(item.Id);
                icon.SetItem(item, _settings, item.Id == _selectedItemId, unseen);

                icon.OpenRequested += OnIconOpenRequested;
                icon.InteractionRequested += OnIconInteraction;
                icon.DragStarted += OnIconDragStarted;
                icon.SelectionChanged += OnIconSelectionChanged;
                //icon.ItemDroppedOn += OnIconDroppedOn;

                // اسمح بالإفلات على الأيقونة
                icon.AllowDrop = true;
                icon.Drop += OnIconDrop;

                wrap.Children.Add(icon);
            }

            groupPanel.Children.Add(wrap);
            PART_Groups.Items.Add(groupPanel);
        }
    }
    private void OnIconSelectionChanged(object? sender, NavItem item)
    {
        _selectedItemId = item.Id;
        // أعِد الرسم لتحديث التظليل
        RenderGroups();
    }
    // ==========================================================
    //  Interactions
    // ==========================================================


    private void OnIconOpenRequested(object? sender, NavItem item)
    {
        if (item is null) return;

        _selectedItemId = item.Id;

        // إذا كان مجلدًا → افتحه (لا نُمرّر للـ Navigation)
        if (item.Kind == NavItemKind.Folder)
        {
            OpenFolder(item);
            return;
        }

        ItemActivated?.Invoke(this, item);
    }

    private void OpenFolder(NavItem folder)
    {
        // افتح نافذة تعرض محتوى المجلد (بأيقونات)
        // مؤقتًا: نُطبّق بحث داخلي
        System.Diagnostics.Debug.WriteLine($"[LauncherView] Opening folder: {folder.Title} ({folder.Children.Count} items)");

        // املأ المجموعات بعناصر المجلد
        Groups.Clear();
        Groups.Add(new LauncherGroupVm
        {
            Title = $"📁 {folder.Title}",
            Items = folder.Children.ToList()
        });
        RenderGroups();
        PART_Status.Text = $"{folder.Children.Count} عنصر في {folder.Title}";
    }

    private void OnIconInteraction(object? sender, LauncherInteractionEventArgs e)
    {
        if (_explorer is null) return;

        switch (e.Action)
        {
            case LauncherAction.Pin:
                _explorer.Pin(e.Item.Id);
                break;
            case LauncherAction.Unpin:
                _explorer.Unpin(e.Item.Id);
                break;
            case LauncherAction.Favorite:
                _explorer.AddToFavorites(e.Item.Id);
                break;
            case LauncherAction.Unfavorite:
                _explorer.RemoveFromFavorites(e.Item.Id);
                break;
            case LauncherAction.Hide:
                _explorer.Hide(e.Item.Id);
                break;
            case LauncherAction.Edit:
                EditRequested?.Invoke(this, e.Item);
                break;
            case LauncherAction.Delete:
                DeleteItem(e.Item);
                break;
        }
    }

    private void DeleteItem(NavItem item)
    {
        if (_explorer is null) return;

        var result = MessageBox.Show(
            $"حذف \"{item.Title}\"؟",
            "تأكيد الحذف",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            // للمجلدات المحلية فقط — نحذف من الشجرة الشخصية
            if (item.Kind == NavItemKind.Folder)
                _explorer.DeleteUserFolder(item.Id);
            else
                _explorer.Hide(item.Id);
        }
    }

    // ==========================================================
    //  Drag & Drop
    // ==========================================================

    private void OnIconDragStarted(object? sender, NavItem item)
    {
        _draggedItem = item;
    }

    private void OnWrapDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("LauncherItem")
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWrapDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("LauncherItem")) return;
        if (_draggedItem is null || _explorer is null) return;

        // أسقط في WrapPanel (نهاية المجموعة)
        var target = (sender as WrapPanel)?.DataContext;
        // لا نعرف الترتيب بدقة — نُرسل إلى النهاية
        _explorer.MoveItemToEnd(_draggedItem.Id);
        _draggedItem = null;
    }

    private void OnIconDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("LauncherItem")) return;
        if (_draggedItem is null || _explorer is null) return;

        var targetIcon = sender as LauncherIcon;
        var target = targetIcon?.Item;
        if (target is null || target.Id == _draggedItem.Id) return;

        // إذا كان الهدف مجلدًا → أضف إليه
        if (target.Kind == NavItemKind.Folder)
        {
            _explorer.AddItemToFolder(_draggedItem.Id, target.Id);
        }
        else
        {
            // اسأل المستخدم: إنشاء مجلد أم ترتيب
            var result = MessageBox.Show(
                $"اسحب \"{_draggedItem.Title}\" على \"{target.Title}\".\n\n" +
                "نعم = إنشاء مجلد يحتويهما\n" +
                "لا = ترتيب فقط",
                "ماذا تريد؟",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _explorer.CreateFolderFromItems(
                    $"مجلد {_draggedItem.Title} + {target.Title}",
                    _draggedItem.Id,
                    target.Id);
            }
            else if (result == MessageBoxResult.No)
            {
                _explorer.MoveItemBefore(_draggedItem.Id, target.Id);
            }
        }

        _draggedItem = null;
        e.Handled = true;
    }

    private void OnIconDroppedOn(object? sender, (NavItem Source, NavItem Target) e)
    {
        // (اختياري)
    }

    // ==========================================================
    //  Search
    // ==========================================================

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        PART_ClearButton.Visibility = string.IsNullOrWhiteSpace(PART_SearchBox.Text)
            ? Visibility.Collapsed : Visibility.Visible;

        var q = PART_SearchBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(q))
        {
            Rebuild();
            return;
        }

        if (_explorer is null) return;

        var results = _explorer.Search(q);
        Groups.Clear();
        Groups.Add(new LauncherGroupVm
        {
            Title = $"🔍 نتائج البحث ({results.Count})",
            Items = results.ToList()
        });
        RenderGroups();
    }

    private void OnClearSearchClicked(object sender, RoutedEventArgs e)
    {
        PART_SearchBox.Text = "";
        PART_SearchBox.Focus();
    }

    // ==========================================================
    //  Toolbar
    // ==========================================================

    private void OnAddClicked(object sender, RoutedEventArgs e)
    {
        if (!_settings.AllowAdding) return;
        AddRequested?.Invoke(this, EventArgs.Empty);
    }
    private void OnSettingsClicked(object sender, RoutedEventArgs e)
        => SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void OnNewFolderClicked(object sender, RoutedEventArgs e)
    {
        if (_explorer is null) return;
        _explorer.CreateUserFolder("مجلد جديد");
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e) => Rebuild();

    private void OnRootDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent("LauncherItem")
            ? DragDropEffects.Move
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnRootDrop(object sender, DragEventArgs e)
    {
        // أي إفلات فارغ = نقل للنهاية
        if (_draggedItem is not null && _explorer is not null)
        {
            _explorer.MoveItemToEnd(_draggedItem.Id);
            _draggedItem = null;
        }
    }
}

public sealed class LauncherGroupVm
{
    public string Title { get; set; } = "";
    public List<NavItem> Items { get; set; } = new();
}
