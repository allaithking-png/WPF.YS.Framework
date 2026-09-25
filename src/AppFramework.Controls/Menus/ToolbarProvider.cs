using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Controls.Menus;

/// <summary>
/// ظ…ط²ظˆظ‘ط¯ ط´ط±ظٹط· ط£ط¯ظˆط§طھ (Toolbar) â€” ط£ط²ط±ط§ط± ط£ظپظ‚ظٹط©.
/// </summary>
public sealed class ToolbarProvider : IMenuProvider
{
    public MenuHost Host => MenuHost.Toolbar;

    public FrameworkElement Build(MenuDefinition definition)
    {
        var toolbar = new ToolBarTray();
        var toolbarBar = new ToolBar();

        // ط¬ظ…ظ‘ط¹ ط§ظ„ط¹ظ†ط§طµط± ط­ط³ط¨ ط§ظ„ظ…ط¬ظ…ظˆط¹ط©
        var groups = definition.Items
            .Where(i => i.IsVisible)
            .GroupBy(i => i.GroupName ?? "")
            .OrderBy(g => g.Min(i => i.Order));

        foreach (var group in groups)
        {
            foreach (var item in group.OrderBy(i => i.Order))
                toolbarBar.Items.Add(BuildToolbarItem(item));

            // ظپط§طµظ„ ط¨ظٹظ† ط§ظ„ظ…ط¬ظ…ظˆط¹ط§طھ
            toolbarBar.Items.Add(new Separator());
        }

        // ط¥ط²ط§ظ„ط© ط¢ط®ط± ظپط§طµظ„
        if (toolbarBar.Items.Count > 0)
            toolbarBar.Items.RemoveAt(toolbarBar.Items.Count - 1);

        toolbar.ToolBars.Add(toolbarBar);
        return toolbar;
    }

    private static FrameworkElement BuildToolbarItem(MenuItemDescriptor d)
    {
        if (d.IsSeparator)
            return new Separator();

        if (d.Children.Count > 0)
        {
            // ظ‚ط§ط¦ظ…ط© ظ…ظ†ط³ط¯ظ„ط©
            var btn = new Button
            {
                Content = BuildButtonContent(d),
                IsEnabled = d.IsEnabled,
                Tag = d
            };

            var menu = new ContextMenu();
            foreach (var child in d.Children.Where(c => c.IsVisible).OrderBy(c => c.Order))
                menu.Items.Add(new MenuItem
                {
                    Header = child.Title,
                    Command = child.Command,
                    CommandParameter = child.CommandParameter,
                    IsEnabled = child.IsEnabled,
                    IsCheckable = child.IsToggle,
                    IsChecked = child.IsChecked
                });

            btn.Click += (_, _) => menu.IsOpen = true;
            btn.ContextMenu = menu;

            return btn;
        }

        var button = new Button
        {
            Content = BuildButtonContent(d),
            Command = d.Command,
            CommandParameter = d.CommandParameter,
            IsEnabled = d.IsEnabled,
            Tag = d,
            ToolTip = d.Description ?? d.Title
        };

        return button;
    }

    private static object BuildButtonContent(MenuItemDescriptor d)
    {
        if (!string.IsNullOrEmpty(d.Icon))
        {
            // ظٹظ…ظƒظ† ط§ط³طھط¨ط¯ط§ظ„ظ‡ط§ ط¨ظ€ Image ط£ظˆ Path ظ„ط§ط­ظ‚ظ‹ط§
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new TextBlock { Text = d.Icon, Margin = new Thickness(0, 0, 6, 0) },
                    new TextBlock { Text = d.Title }
                }
            };
        }

        return d.Title;
    }
}
