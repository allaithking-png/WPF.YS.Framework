using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AppFramework.Controls.Converters;

/// <summary>bool → Visibility (true = Visible، false = Collapsed).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>int → Collapsed إذا كان 0، وإلا Visible.</summary>
public sealed class ZeroToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int n)
            return n == 0 ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}