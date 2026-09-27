using System;
using System.Windows;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

public partial class EditItemDialog : Window
{
    private readonly NavItem _item;

    public NavItem? Result { get; private set; }

    public EditItemDialog(NavItem item)
    {
        InitializeComponent();
        _item = item;

        PART_TitleBox.Text = item.Title;
        PART_IconBox.Text = item.Icon ?? "";
        PART_SizeBox.Text = item.CustomIconSize?.ToString() ?? "";
        PART_ColorBox.Text = item.CustomBackground ?? "";
    }
    private void OnPickIconClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new IconPickerDialog(PART_IconBox.Text);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.Result))
        {
            PART_IconBox.Text = dialog.Result;
        }
    }
    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        var title = PART_TitleBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(title))
        {
            MessageBox.Show("أدخل عنوانًا.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _item.Title = title;
        _item.Icon = PART_IconBox.Text?.Trim();

        if (double.TryParse(PART_SizeBox.Text?.Trim(), out var size))
            _item.CustomIconSize = size;
        else
            _item.CustomIconSize = null;

        _item.CustomBackground = string.IsNullOrWhiteSpace(PART_ColorBox.Text)
            ? null
            : PART_ColorBox.Text.Trim();

        Result = _item;
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
