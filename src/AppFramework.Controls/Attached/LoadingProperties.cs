using System.Windows;
using System.Windows.Controls;

namespace AppFramework.Controls.Attached;

/// <summary>
/// خاصية مرفقة لإظهار حالة التحميل على أي عنصر.
/// </summary>
/// <remarks>
/// عند تفعيل IsLoading، يُعطَّل العنصر ويُظهر مؤشر تحميل.
/// </remarks>
public static class LoadingProperties
{
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.RegisterAttached(
            "IsLoading",
            typeof(bool),
            typeof(LoadingProperties),
            new PropertyMetadata(false, OnIsLoadingChanged));

    public static bool GetIsLoading(DependencyObject obj)
        => (bool)obj.GetValue(IsLoadingProperty);

    public static void SetIsLoading(DependencyObject obj, bool value)
        => obj.SetValue(IsLoadingProperty, value);

    // ==========================================================

    public static readonly DependencyProperty LoadingTextProperty =
        DependencyProperty.RegisterAttached(
            "LoadingText",
            typeof(string),
            typeof(LoadingProperties),
            new PropertyMetadata("جارٍ التحميل..."));

    public static string GetLoadingText(DependencyObject obj)
        => (string)obj.GetValue(LoadingTextProperty);

    public static void SetLoadingText(DependencyObject obj, string value)
        => obj.SetValue(LoadingTextProperty, value);

    // ==========================================================

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        var isLoading = (bool)e.NewValue;

        // عطّل العنصر أثناء التحميل
        if (element is Control control)
        {
            control.IsEnabled = !isLoading;
        }

        // بدّل المؤشر
        if (element is FrameworkElement fe)
        {
            fe.Cursor = isLoading
                ? System.Windows.Input.Cursors.Wait
                : System.Windows.Input.Cursors.Arrow;
        }
    }
}