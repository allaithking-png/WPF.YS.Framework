using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AppFramework.Controls.Attached;

/// <summary>
/// خصائص مرفقة لدعم التحقق البصري من صحة البيانات.
/// </summary>
public static class ValidationProperties
{
    // ==========================================================
    //  IsValid (bool)
    // ==========================================================

    public static readonly DependencyProperty IsValidProperty =
        DependencyProperty.RegisterAttached(
            "IsValid",
            typeof(bool),
            typeof(ValidationProperties),
            new PropertyMetadata(true, OnIsValidChanged));

    public static bool GetIsValid(DependencyObject obj)
        => (bool)obj.GetValue(IsValidProperty);

    public static void SetIsValid(DependencyObject obj, bool value)
        => obj.SetValue(IsValidProperty, value);

    // ==========================================================
    //  ErrorMessage (string)
    // ==========================================================

    public static readonly DependencyProperty ErrorMessageProperty =
        DependencyProperty.RegisterAttached(
            "ErrorMessage",
            typeof(string),
            typeof(ValidationProperties),
            new PropertyMetadata(null));

    public static string? GetErrorMessage(DependencyObject obj)
        => (string?)obj.GetValue(ErrorMessageProperty);

    public static void SetErrorMessage(DependencyObject obj, string? value)
        => obj.SetValue(ErrorMessageProperty, value);

    // ==========================================================
    //  ErrorBrush (Brush)
    // ==========================================================

    public static readonly DependencyProperty ErrorBrushProperty =
        DependencyProperty.RegisterAttached(
            "ErrorBrush",
            typeof(Brush),
            typeof(ValidationProperties),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xE5, 0x3E, 0x3E))));

    public static Brush GetErrorBrush(DependencyObject obj)
        => (Brush)obj.GetValue(ErrorBrushProperty);

    public static void SetErrorBrush(DependencyObject obj, Brush value)
        => obj.SetValue(ErrorBrushProperty, value);

    // ==========================================================
    //  Handler
    // ==========================================================

    private static void OnIsValidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Control control) return;

        var isValid = (bool)e.NewValue;

        if (isValid)
        {
            // امسح الحدود
            control.ClearValue(Control.BorderBrushProperty);
            control.ClearValue(Control.BorderThicknessProperty);
            control.ToolTip = null;
        }
        else
        {
            var brush = GetErrorBrush(control);
            control.BorderBrush = brush;
            control.BorderThickness = new Thickness(1.5);

            var msg = GetErrorMessage(control);
            if (!string.IsNullOrEmpty(msg))
                control.ToolTip = msg;
        }
    }
}