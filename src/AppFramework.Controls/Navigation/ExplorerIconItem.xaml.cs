using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

public partial class ExplorerIconItem : UserControl
{
    public ExplorerIconItem()
    {
        InitializeComponent();

       

        DataContextChanged += OnDataContextChanged;

        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
        MouseLeftButtonDown += OnMouseDown;
        MouseLeftButtonUp += OnMouseUp;
        MouseDoubleClick += OnMouseDoubleClick;
    }
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is NavItem item)
            SetItem(item);
    }

    // ==========================================================
    //  NavItem
    // ==========================================================

    public NavItem? NavItem { get; private set; }

    public void SetItem(NavItem item)
    {
        NavItem = item;

        PART_Icon.Text = item.Icon ?? DefaultIconFor(item.Kind);
        PART_Title.Text = item.Title;

        PART_StarIcon.Visibility = item.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
        PART_PinIcon.Visibility = item.IsPinned ? Visibility.Visible : Visibility.Collapsed;

        if (item.UsageCount > 0)
        {
            PART_UsageCount.Text = $"({item.UsageCount})";
            PART_UsageCount.Visibility = Visibility.Visible;
        }
        else
        {
            PART_UsageCount.Visibility = Visibility.Collapsed;
        }
    }

    private static string DefaultIconFor(NavItemKind kind) => kind switch
    {
        NavItemKind.Screen => "🖥️",
        NavItemKind.Report => "📊",
        NavItemKind.Folder => "📁",
        NavItemKind.ExternalFile => "📄",
        NavItemKind.ExternalFolder => "📁",
        NavItemKind.Url => "🔗",
        _ => "•"
    };

    // ==========================================================
    //  Hover Effects
    // ==========================================================

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        PART_Root.Background = (System.Windows.Media.Brush)FindResource("App.SurfaceBrush");
        PART_Root.BorderBrush = (System.Windows.Media.Brush)FindResource("App.BorderBrush");
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        PART_Root.Background = System.Windows.Media.Brushes.Transparent;
        PART_Root.BorderBrush = System.Windows.Media.Brushes.Transparent;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var scale = new DoubleAnimation(1, 0.95, TimeSpan.FromMilliseconds(80));
        PART_Root.RenderTransformOrigin = new Point(0.5, 0.5);
        PART_Root.RenderTransform = new System.Windows.Media.ScaleTransform(1, 1);
        ((System.Windows.Media.ScaleTransform)PART_Root.RenderTransform)
            .BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scale);
        ((System.Windows.Media.ScaleTransform)PART_Root.RenderTransform)
            .BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scale);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var scale = new DoubleAnimation(0.95, 1, TimeSpan.FromMilliseconds(80));
        if (PART_Root.RenderTransform is System.Windows.Media.ScaleTransform st)
        {
            st.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scale);
            st.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scale);
        }
    }

    // ==========================================================
    //  Events
    // ==========================================================

    /// <summary>يُطلق عند نقر مزدوج.</summary>
    public event EventHandler<NavItem>? Activated;

    /// <summary>يُطلق عند طلب تثبيت/إلغاء.</summary>
    public event EventHandler<NavItem>? TogglePinRequested;

    /// <summary>يُطلق عند طلب مفضلة/إلغاء.</summary>
    public event EventHandler<NavItem>? ToggleFavoriteRequested;

    /// <summary>يُطلق عند طلب إخفاء.</summary>
    public event EventHandler<NavItem>? HideRequested;

    private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NavItem is null) return;
        if (NavItem.Kind == NavItemKind.Folder) return;

        Activated?.Invoke(this, NavItem);
    }

    private void OnOpenClicked(object sender, RoutedEventArgs e)
    {
        if (NavItem is null) return;
        Activated?.Invoke(this, NavItem);
    }

    private void OnTogglePinClicked(object sender, RoutedEventArgs e)
    {
        if (NavItem is null) return;
        TogglePinRequested?.Invoke(this, NavItem);
    }

    private void OnToggleFavoriteClicked(object sender, RoutedEventArgs e)
    {
        if (NavItem is null) return;
        ToggleFavoriteRequested?.Invoke(this, NavItem);
    }

    private void OnHideClicked(object sender, RoutedEventArgs e)
    {
        if (NavItem is null) return;
        HideRequested?.Invoke(this, NavItem);
    }
}
