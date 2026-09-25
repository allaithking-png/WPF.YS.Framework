using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Controls.Menus;

/// <summary>
/// ظ…ط²ظˆظ‘ط¯ ظ‚ظˆط§ط¦ظ… ظƒظ„ط§ط³ظٹظƒظٹ (MenuBar) â€” ظ…ط«ظ„ File / Edit / View.
/// </summary>
public sealed class MenuBarProvider : IMenuProvider
{
    public MenuHost Host => MenuHost.MenuBar;

    public FrameworkElement Build(MenuDefinition definition)
    {
        var menu = new System.Windows.Controls.Menu
        {
            IsMainMenu = true
        };

        // ط¬ظ…ظ‘ط¹ ط§ظ„ط¹ظ†ط§طµط± ط­ط³ط¨ GroupName
        var groups = definition.Items
            .Where(i => i.IsVisible)
            .GroupBy(i => i.GroupName ?? "");

        foreach (var group in groups)
        {
            // ظ„ظˆ ط§ظ„ظ…ط¬ظ…ظˆط¹ط© ظ†ظپط³ظ‡ط§ ط¹ظ†طµط± ظˆط§ط­ط¯ ظ„ظ‡ ط£ط¨ظ†ط§ط، -> ط§ط¬ط¹ظ„ظ‡ط§ ظ‚ط§ط¦ظ…ط© ط¹ظ„ظٹط§
            if (string.IsNullOrEmpty(group.Key) && group.Count() == 1)
            {
                var single = group.First();
                menu.Items.Add(BuildMenuItem(single));
            }
            else
            {
                // ظ…ط¬ظ…ظˆط¹ط© ظ…طھط¹ط¯ط¯ط© -> ظ‚ط§ط¦ظ…ط© ط¹ظ„ظٹط§ ظˆط§ط­ط¯ط© طھط­ظ…ظ„ ط§ط³ظ… ط§ظ„ظ…ط¬ظ…ظˆط¹ط©
                var top = new MenuItem
                {
                    Header = string.IsNullOrEmpty(group.Key) ? "ط£ظˆط§ظ…ط±" : group.Key
                };

                foreach (var item in group.OrderBy(i => i.Order))
                    top.Items.Add(BuildMenuItem(item));

                menu.Items.Add(top);
            }
        }

        return menu;
    }

    private static MenuItem BuildMenuItem(MenuItemDescriptor d)
    {
        var mi = new MenuItem
        {
            Header = d.Title,
            Command = d.Command,
            CommandParameter = d.CommandParameter,
            IsEnabled = d.IsEnabled,
            Visibility = d.IsVisible ? Visibility.Visible : Visibility.Collapsed,
            IsCheckable = d.IsToggle,
            IsChecked = d.IsChecked,
            Tag = d
        };

        if (!string.IsNullOrEmpty(d.Icon))
        {
            // ط§ظ„ط£ظٹظ‚ظˆظ†ط© طھظڈط¶ط§ظپ ظ„ط§ط­ظ‚ظ‹ط§ ظƒطµظˆط±ط© ط£ظˆ glyph â€” ظ‡ظ†ط§ ظ†ط¶ط¹ ط§ظ„ظ†طµ ظƒط¨ط¯ظٹظ„
            mi.Icon = new TextBlock { Text = d.Icon };
        }

        if (d.IsSeparator)
        {
            return new MenuItem { Header = "-", IsEnabled = false };
        }

        foreach (var child in d.Children.Where(c => c.IsVisible).OrderBy(c => c.Order))
            mi.Items.Add(BuildMenuItem(child));

        return mi;
    }
}
