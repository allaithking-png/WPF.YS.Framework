using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace AppFramework.Controls.Attached;

/// <summary>
/// خاصية مرفقة لإضافة أيقونة لأي عنصر (Button, MenuItem, ...).
/// </summary>
/// <remarks>
/// الاستخدام:
/// <code>
/// &lt;Button local:IconProperties.Icon="Save" 
///         local:IconProperties.Position="Left"
///         local:IconProperties.IconSize="16"
///         Content="حفظ"/&gt;
/// </code>
/// </remarks>
public static class IconProperties
{
    // ==========================================================
    //  Icon (string glyph/name)
    // ==========================================================

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.RegisterAttached(
            "Icon",
            typeof(string),
            typeof(IconProperties),
            new PropertyMetadata(null, OnIconChanged));

    public static string? GetIcon(DependencyObject obj)
        => (string?)obj.GetValue(IconProperty);

    public static void SetIcon(DependencyObject obj, string? value)
        => obj.SetValue(IconProperty, value);

    // ==========================================================
    //  Position
    // ==========================================================

    public enum IconPosition { Left, Right, Top, Bottom }

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.RegisterAttached(
            "Position",
            typeof(IconPosition),
            typeof(IconProperties),
            new PropertyMetadata(IconPosition.Left));

    public static IconPosition GetPosition(DependencyObject obj)
        => (IconPosition)obj.GetValue(PositionProperty);

    public static void SetPosition(DependencyObject obj, IconPosition value)
        => obj.SetValue(PositionProperty, value);

    // ==========================================================
    //  IconSize
    // ==========================================================

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.RegisterAttached(
            "IconSize",
            typeof(double),
            typeof(IconProperties),
            new PropertyMetadata(16.0));

    public static double GetIconSize(DependencyObject obj)
        => (double)obj.GetValue(IconSizeProperty);

    public static void SetIconSize(DependencyObject obj, double value)
        => obj.SetValue(IconSizeProperty, value);

    // ==========================================================
    //  Change Handler
    // ==========================================================

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ContentControl cc) return;

        var icon = e.NewValue as string;
        var position = GetPosition(cc);
        var size = GetIconSize(cc);
        var content = cc.Content;

        // إذا كان الـ Content هو سبقًا الـ StackPanel الذي أنشأناه، لا نُكرّر
        if (content is StackPanel sp && sp.Tag as string == "IconProperties.Wrapper")
            return;

        if (string.IsNullOrEmpty(icon))
        {
            // لا أيقونة — أزل الـ Wrapper إن وُجد
            if (content is StackPanel wrapper && wrapper.Tag as string == "IconProperties.Wrapper")
            {
                var originalContent = wrapper.Children
                    .OfType<FrameworkElement>()
                    .FirstOrDefault(x => (x as FrameworkElement)?.Tag as string == "IconProperties.Content");
                cc.Content = originalContent?.DataContext ?? null;
            }
            return;
        }

        var iconBlock = new TextBlock
        {
            Text = icon,
            FontSize = size,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = position switch
            {
                IconPosition.Left => new Thickness(0, 0, 6, 0),
                IconPosition.Right => new Thickness(6, 0, 0, 0),
                IconPosition.Top => new Thickness(0, 0, 0, 4),
                IconPosition.Bottom => new Thickness(0, 4, 0, 0),
                _ => new Thickness(0)
            }
        };

        var contentBlock = new ContentPresenter
        {
            Content = content,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Tag = "IconProperties.Content"
        };

        var panel = position switch
        {
            IconPosition.Left or IconPosition.Right => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            _ => new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        panel.Tag = "IconProperties.Wrapper";

        if (position == IconPosition.Left || position == IconPosition.Top)
        {
            panel.Children.Add(iconBlock);
            panel.Children.Add(contentBlock);
        }
        else
        {
            panel.Children.Add(contentBlock);
            panel.Children.Add(iconBlock);
        }

        cc.Content = panel;
    }
}
