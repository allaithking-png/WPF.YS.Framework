using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace AppFramework.Controls.Screens;

/// <summary>
/// حقل نموذج واحد — يحتوي على Label + Input.
/// </summary>
public sealed class FormField
{
    public string Label { get; }
    public StackPanel Container { get; }
    private readonly PropertyInfo _property;
    private readonly object _target;
    private readonly Func<object?> _getValue;

    public FormField(string label, PropertyInfo property, object target, StackPanel container, Func<object?> getValue)
    {
        Label = label;
        _property = property;
        _target = target;
        Container = container;
        _getValue = getValue;
    }

    public bool Validate(out string error)
    {
        error = "";
        var value = _getValue();

        // تحقق أساسي: null/empty للنصوص
        if (_property.PropertyType == typeof(string))
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
            {
                error = "لا يمكن أن يكون فارغًا";
                return false;
            }
        }

        return true;
    }

    public void Apply()
    {
        var value = _getValue();
        _property.SetValue(_target, value);
    }
}

/// <summary>
/// يبني حقول النموذج ديناميكيًا من نوع DTO.
/// </summary>
public static class FormFieldBuilder
{
    public static FormField? CreateField(PropertyInfo prop, object target)
    {
        var type = prop.PropertyType;
        var currentValue = prop.GetValue(target);

        var container = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

        // Label
        var label = new TextBlock
        {
            Text = FormatLabel(prop.Name),
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 4)
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, "App.TextPrimaryBrush");
        container.Children.Add(label);

        // Input
        FrameworkElement input;
        Func<object?> getValue;

        if (type == typeof(string))
        {
            var tb = new TextBox { Text = currentValue as string ?? "" };
            input = tb;
            getValue = () => tb.Text;
        }
        else if (type == typeof(int) || type == typeof(long))
        {
            var tb = new TextBox { Text = currentValue?.ToString() ?? "0" };
            input = tb;
            getValue = () =>
            {
                if (long.TryParse(tb.Text, out var l))
                    return type == typeof(int) ? (int)l : l;
                return type == typeof(int) ? 0 : 0L;
            };
        }
        else if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            var tb = new TextBox { Text = currentValue?.ToString() ?? "0" };
            input = tb;
            getValue = () =>
            {
                if (decimal.TryParse(tb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    if (type == typeof(decimal)) return d;
                    if (type == typeof(double)) return (double)d;
                    return (float)d;
                }
                return type == typeof(decimal) ? 0m : 0.0;
            };
        }
        else if (type == typeof(bool))
        {
            var cb = new CheckBox { IsChecked = currentValue is true };
            input = cb;
            getValue = () => cb.IsChecked ?? false;
        }
        else if (type == typeof(DateTime))
        {
            var dp = new DatePicker { SelectedDate = currentValue as DateTime? };
            input = dp;
            getValue = () => dp.SelectedDate ?? DateTime.UtcNow;
        }
        else if (type.IsEnum)
        {
            var cb = new ComboBox();
            foreach (var val in Enum.GetValues(type))
                cb.Items.Add(val);
            cb.SelectedItem = currentValue;
            input = cb;
            getValue = () => cb.SelectedItem ?? Enum.GetValues(type).GetValue(0);
        }
        else
        {
            // نوع غير مدعوم
            var tb = new TextBox
            {
                Text = currentValue?.ToString() ?? "",
                IsEnabled = false
            };
            input = tb;
            getValue = () => currentValue;
        }

        input.MinHeight = 32;
        container.Children.Add(input);

        return new FormField(FormatLabel(prop.Name), prop, target, container, getValue);
    }

    private static string FormatLabel(string name)
    {
        // CamelCase → "Camel Case"
        var result = "";
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result += " ";
            result += name[i];
        }
        return result;
    }
}