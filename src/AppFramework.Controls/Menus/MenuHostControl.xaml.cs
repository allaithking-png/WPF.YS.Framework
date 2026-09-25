using System;
using System.Windows;
using System.Windows.Controls;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Services;

namespace AppFramework.Controls.Menus;

/// <summary>
/// UserControl ذكي يعرض القائمة الحالية بناءً على MenuHost المفضّل.
/// يستمع لحدث MenuChanged من IMenuManager ويُحدّث نفسه.
/// </summary>
public partial class MenuHostControl : UserControl
{
    private IMenuManager? _menuManager;
    //private MenuProviderRegistry? _registry;
    //private IDisposable? _subscription;

    public MenuHostControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ==========================================================
    //  Dependency Properties
    // ==========================================================

    /// <summary>خدمة MenuManager (تُحقن إن لم تُمرّر).</summary>
    public static readonly DependencyProperty MenuManagerProperty =
        DependencyProperty.Register(
            nameof(MenuManager),
            typeof(IMenuManager),
            typeof(MenuHostControl),
            new PropertyMetadata(null, OnMenuManagerChanged));

    public IMenuManager? MenuManager
    {
        get => (IMenuManager?)GetValue(MenuManagerProperty);
        set => SetValue(MenuManagerProperty, value);
    }

    /// <summary>سجل مزوّدي القوائم.</summary>
    public static readonly DependencyProperty ProviderRegistryProperty =
        DependencyProperty.Register(
            nameof(ProviderRegistry),
            typeof(MenuProviderRegistry),
            typeof(MenuHostControl),
            new PropertyMetadata(null, OnRegistryChanged));

    public MenuProviderRegistry? ProviderRegistry
    {
        get => (MenuProviderRegistry?)GetValue(ProviderRegistryProperty);
        set => SetValue(ProviderRegistryProperty, value);
    }

    /// <summary>نوع العرض المفضّل (MenuBar / Toolbar / Ribbon / ...).</summary>
    public static readonly DependencyProperty PreferredHostProperty =
        DependencyProperty.Register(
            nameof(PreferredHost),
            typeof(MenuHost),
            typeof(MenuHostControl),
            new PropertyMetadata(MenuHost.MenuBar, OnPreferredHostChanged));

    public MenuHost PreferredHost
    {
        get => (MenuHost)GetValue(PreferredHostProperty);
        set => SetValue(PreferredHostProperty, value);
    }

    // ==========================================================
    //  Lifecycle
    // ==========================================================

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachToManager();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachFromManager();
    }

    private static void OnMenuManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuHostControl ctl && ctl.IsLoaded)
        {
            ctl.DetachFromManager();
            ctl.AttachToManager();
        }
    }

    private static void OnRegistryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuHostControl ctl && ctl.IsLoaded)
        {
            ctl.Rebuild();
        }
    }

    private static void OnPreferredHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuHostControl ctl && ctl.IsLoaded)
        {
            ctl.Rebuild();
        }
    }

    // ==========================================================
    //  Attach / Detach
    // ==========================================================

    private void AttachToManager()
    {
        if (MenuManager is null) return;

        _menuManager = MenuManager;
        _menuManager.MenuChanged += OnMenuChanged;

        // اعرض القائمة الحالية (إن وُجدت)
        if (_menuManager.Current is not null)
            RenderDefinition(_menuManager.Current);
    }

    private void DetachFromManager()
    {
        if (_menuManager is not null)
        {
            _menuManager.MenuChanged -= OnMenuChanged;
            _menuManager = null;
        }
    }

    private void OnMenuChanged(object? sender, MenuDefinition definition)
    {
        // نواجه الخيط الصحيح (قد يأتي الحدث من خيط مختلف)
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => RenderDefinition(definition));
            return;
        }

        RenderDefinition(definition);
    }

    // ==========================================================
    //  Rendering
    // ==========================================================

    private void Rebuild()
    {
        if (_menuManager?.Current is not null)
            RenderDefinition(_menuManager.Current);
    }

    private void RenderDefinition(MenuDefinition definition)
    {
        var registry = ProviderRegistry ?? MenuProviderRegistry.CreateDefault();

        // اختر المضيف: إما المفضّل من الـ View، أو من الـ Definition
        var host = PreferredHost;
        if (definition.Host != host && registry.Supports(definition.Host))
            host = definition.Host;

        var provider = registry.Get(host);
        if (provider is null)
        {
            PART_Host.Content = new TextBlock
            {
                Text = $"لا يوجد مزوّد قوائم لـ {host}",
                Margin = new Thickness(8),
                FontStyle = FontStyles.Italic
            };
            return;
        }

        try
        {
            var element = provider.Build(definition);
            PART_Host.Content = element;
        }
        catch (Exception ex)
        {
            PART_Host.Content = new TextBlock
            {
                Text = $"خطأ في بناء القائمة: {ex.Message}",
                Margin = new Thickness(8),
                Foreground = System.Windows.Media.Brushes.Red
            };
        }
    }
}