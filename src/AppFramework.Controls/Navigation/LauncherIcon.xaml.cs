using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

public partial class LauncherIcon : UserControl
{
    private LauncherSettings _settings = new();
    private readonly DispatcherTimer _longPressTimer;
    private readonly DispatcherTimer _clickTimer;
    private bool _longPressTriggered;
    private Point _dragStartPoint;
    private bool _isPressed;

    public LauncherIcon()
    {
        InitializeComponent();

        _longPressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _longPressTimer.Tick += OnLongPressTick;

        _clickTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _clickTimer.Tick += OnClickTimerTick;
    }

    // ==========================================================
    //  Events
    // ==========================================================

    public event EventHandler<NavItem>? OpenRequested;
    public event EventHandler<NavItem>? SelectionChanged;
    public event EventHandler<LauncherInteractionEventArgs>? InteractionRequested;
    public event EventHandler<NavItem>? DragStarted;

    // ==========================================================
    //  State
    // ==========================================================

    public NavItem? Item { get; private set; }
    public bool IsSelected { get; private set; }

    // ==========================================================
    //  Setup
    // ==========================================================

    public void SetItem(NavItem item, LauncherSettings settings, bool isSelected, int unseenCount = 0)
    {
        Item = item;
        _settings = settings ?? new LauncherSettings();
        IsSelected = isSelected;

        var iconSize = item.CustomIconSize ?? _settings.IconSize;
        var innerSize = Math.Max(32, iconSize - 20);

        PART_IconBorder.Width = iconSize;
        PART_IconBorder.Height = iconSize;
        PART_Icon.FontSize = innerSize * 0.6;

        if (!string.IsNullOrEmpty(item.CustomBackground))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(item.CustomBackground);
                PART_IconBorder.Background = new SolidColorBrush(color);
            }
            catch { }
        }
        else
        {
            PART_IconBorder.SetResourceReference(Border.BackgroundProperty, "App.SurfaceBrush");
        }

        PART_Icon.Text = item.Icon ?? "•";

        PART_Title.Text = item.Title;
        PART_Title.FontSize = _settings.LabelFontSize;
        PART_Title.Visibility = _settings.ShowLabels ? Visibility.Visible : Visibility.Collapsed;
        PART_Title.MaxWidth = iconSize + 20;

        PART_Star.Visibility = item.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
        PART_Pin.Visibility = item.IsPinned ? Visibility.Visible : Visibility.Collapsed;

        if (item.UsageCount > 0)
        {
            PART_UsageCount.Text = $"({item.UsageCount})";
            PART_UsageCount.Visibility = Visibility.Visible;
        }
        else
        {
            PART_UsageCount.Visibility = Visibility.Collapsed;
        }
        // Badges
        if (_settings.ShowBadges)
        {
            PART_NewBadge.Visibility = item.IsNew ? Visibility.Visible : Visibility.Collapsed;
            PART_UpdatedBadge.Visibility = (!item.IsNew && item.IsUpdated)
                ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            PART_NewBadge.Visibility = Visibility.Collapsed;
            PART_UpdatedBadge.Visibility = Visibility.Collapsed;
        }
        // Border
        PART_IconBorder.BorderThickness = new Thickness(2);
        if (item.IsActive)
            PART_IconBorder.BorderBrush = (Brush)FindResource("App.PrimaryBrush");
        else if (isSelected)
            PART_IconBorder.BorderBrush = (Brush)FindResource("App.BorderBrush");
        else
            PART_IconBorder.BorderBrush = Brushes.Transparent;

        // ✅ History Badge
        if (_settings.ShowUnseenCountBadge && unseenCount > 0)
        {
            PART_HistoryCount.Text = unseenCount > 99 ? "99+" : unseenCount.ToString();
            PART_HistoryBadge.Visibility = Visibility.Visible;
        }
        else
        {
            PART_HistoryBadge.Visibility = Visibility.Collapsed;
        }
    }

    // ==========================================================
    //  Mouse
    // ==========================================================

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Item is null) return;

        _isPressed = true;
        _dragStartPoint = e.GetPosition(this);
        _longPressTriggered = false;

        _longPressTimer.Interval = TimeSpan.FromMilliseconds(_settings.LongPressMs);
        _longPressTimer.Start();

        if (e.ClickCount == 1)
        {
            _clickTimer.Stop();
            _clickTimer.Start();
        }

        var scale = new DoubleAnimation(1, 0.92, TimeSpan.FromMilliseconds(80));
        PART_Layout.RenderTransformOrigin = new Point(0.5, 0.5);
        PART_Layout.RenderTransform ??= new ScaleTransform(1, 1);
        if (PART_Layout.RenderTransform is ScaleTransform st)
        {
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        }
    }

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Item is null) return;

        _clickTimer.Stop();
        _longPressTimer.Stop();
        _isPressed = false;

        // أعِد الحجم
        var scale = new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(80));
        if (PART_Layout.RenderTransform is ScaleTransform st)
        {
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        }

        System.Diagnostics.Debug.WriteLine($"[LauncherIcon] DoubleClick: {Item.Title}");

        OpenRequested?.Invoke(this, Item);
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPressed || Item is null) return;
        if (!_settings.EnableDragAndDrop) return;

        var current = e.GetPosition(this);
        var diff = current - _dragStartPoint;

        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _longPressTimer.Stop();
        _clickTimer.Stop();
        _longPressTriggered = true;

        try
        {
            DragStarted?.Invoke(this, Item);
            DragDrop.DoDragDrop(this, new DataObject("LauncherItem", Item), DragDropEffects.Move);
        }
        catch { }
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Item is null) return;
        _longPressTimer.Stop();
        _clickTimer.Stop();
        ShowContextMenu();
        e.Handled = true;
    }

    // ==========================================================
    //  Timers
    // ==========================================================

    private void OnClickTimerTick(object? sender, EventArgs e)
    {
        _clickTimer.Stop();

        if (_longPressTriggered) return;
        if (Item is null) return;

        System.Diagnostics.Debug.WriteLine($"[LauncherIcon] SingleClick: {Item.Title}");

        switch (_settings.ClickPolicy)
        {
            case ClickPolicy.SingleClickToOpen:
                OpenRequested?.Invoke(this, Item);
                break;

            case ClickPolicy.DoubleClickToOpen:
            case ClickPolicy.SelectOnly:
                IsSelected = true;
                SelectionChanged?.Invoke(this, Item);
                break;
        }
    }

    private void OnLongPressTick(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        if (Item is null) return;

        _longPressTriggered = true;
        ShowContextMenu();
    }

    // ==========================================================
    //  Context Menu
    // ==========================================================

    private void ShowContextMenu()
    {
        if (Item is null) return;
        var menu = BuildContextMenu(Item);
        menu.PlacementTarget = this;
        menu.IsOpen = true;
    }

    private ContextMenu BuildContextMenu(NavItem item)
    {
        var menu = new ContextMenu();
        var s = _settings;
        // فتح (دائمًا)
        if (item.Kind != NavItemKind.Folder)
        {
            menu.Items.Add(MakeMenuItem("📂 فتح", () => OpenRequested?.Invoke(this, item)));
            menu.Items.Add(new Separator());
        }

        // تعديل (إن كان مسموحًا)
        if (s.AllowEditing)
        {  menu.Items.Add(MakeMenuItem("✏️ تعديل", () =>
            InteractionRequested?.Invoke(this, new LauncherInteractionEventArgs(item, LauncherAction.Edit))));
        }
        // حذف (إن كان مسموحًا)
        if (s.AllowDeleting)
        {
            menu.Items.Add(MakeMenuItem("🗑️ حذف", () =>
            InteractionRequested?.Invoke(this, new LauncherInteractionEventArgs(item, LauncherAction.Delete))));
        }
        if (s.AllowEditing || s.AllowDeleting)
        {
            menu.Items.Add(new Separator());
        }
        // تثبيت (إن كان مسموحًا)
        if (s.AllowPinning)
        {
            menu.Items.Add(MakeMenuItem(item.IsPinned ? "📌 إلغاء التثبيت" : "📌 تثبيت", () =>
            InteractionRequested?.Invoke(this, new LauncherInteractionEventArgs(item,
                item.IsPinned ? LauncherAction.Unpin : LauncherAction.Pin))));
        }
        // مفضلة (إن كان مسموحًا)
        if (s.AllowFavorites)
        {
            menu.Items.Add(MakeMenuItem(item.IsFavorite ? "⭐ إزالة من المفضلة" : "⭐ إضافة للمفضلة", () =>
            InteractionRequested?.Invoke(this, new LauncherInteractionEventArgs(item,
                item.IsFavorite ? LauncherAction.Unfavorite : LauncherAction.Favorite))));
        }
        // إخفاء (إن كان مسموحًا)
        if (s.AllowHiding)
        {
            menu.Items.Add(new Separator());

            menu.Items.Add(MakeMenuItem("🚫 إخفاء", () =>
                InteractionRequested?.Invoke(this, new LauncherInteractionEventArgs(item, LauncherAction.Hide))));
        }
        return menu;
    }

    private static MenuItem MakeMenuItem(string header, Action onClick)
    {
        var mi = new MenuItem { Header = header };
        mi.Click += (_, _) => onClick();
        return mi;
    }

    private void OnMenuButtonClicked(object sender, RoutedEventArgs e)
    {
        ShowContextMenu();
        e.Handled = true;
    }
}
