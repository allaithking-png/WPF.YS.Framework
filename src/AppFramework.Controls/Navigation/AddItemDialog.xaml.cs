using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

public partial class AddItemDialog : Window
{
    private readonly List<NavItem> _availableScreens;
    private readonly List<NavItem> _availableReports;
    private readonly List<NavItem> _folders;

    public AddItemDialogResult? Result { get; private set; }

    public AddItemDialog(
        IEnumerable<NavItem> folders,
        IEnumerable<NavItem> availableScreens,
        IEnumerable<NavItem> availableReports)
    {
        InitializeComponent();

        _folders = folders.Where(f => f.Kind == NavItemKind.Folder).ToList();
        _availableScreens = availableScreens.Where(s => s.Kind == NavItemKind.Screen).ToList();
        _availableReports = availableReports.Where(r => r.Kind == NavItemKind.Report).ToList();

        // املأ قائمة المجلدات
        PART_FolderCombo.Items.Add(new NavItem { Id = "", Title = "(بدون)" });
        foreach (var f in _folders)
            PART_FolderCombo.Items.Add(f);
        PART_FolderCombo.SelectedIndex = 0;

        PART_KindCombo.SelectedIndex = 0;
        OnKindChanged(null!, null!);
    }

    // ==========================================================
    //  UI Events
    // ==========================================================

    private void OnKindChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PART_KindCombo.SelectedItem is not ComboBoxItem item) return;
        var kind = item.Tag as string;

        // اظهر/اخفِ حسب النوع
        var isScreenOrReport = kind == "Screen" || kind == "Report";
        PART_TargetCombo.Visibility = isScreenOrReport ? Visibility.Visible : Visibility.Collapsed;
        PART_TargetBoxPanel.Visibility = isScreenOrReport ? Visibility.Collapsed : Visibility.Visible;

        PART_BrowseButton.Visibility = (kind == "ExternalFile" || kind == "ExternalFolder")
            ? Visibility.Visible : Visibility.Collapsed;

        // املأ ComboBox
        PART_TargetCombo.Items.Clear();

        if (kind == "Screen")
        {
            PART_TargetLabel.Text = "اختر الشاشة";
            foreach (var s in _availableScreens)
                PART_TargetCombo.Items.Add(new PickerItem { Id = s.Id, DisplayName = s.Title });
        }
        else if (kind == "Report")
        {
            PART_TargetLabel.Text = "اختر التقرير";
            foreach (var r in _availableReports)
                PART_TargetCombo.Items.Add(new PickerItem { Id = r.Id, DisplayName = r.Title });
        }
        else
        {
            PART_TargetLabel.Text = kind switch
            {
                "Url" => "الرابط",
                "ExternalFile" => "مسار الملف",
                "ExternalFolder" => "مسار المجلد",
                "LocalFolder" => "(لا يوجد)",
                _ => "الهدف"
            };

            PART_TargetBox.IsEnabled = kind != "LocalFolder";
        }

        if (PART_TargetCombo.Items.Count > 0)
            PART_TargetCombo.SelectedIndex = 0;

        // املأ العنوان تلقائيًا
        if (kind == "Screen" || kind == "Report")
        {
            if (PART_TargetCombo.SelectedItem is PickerItem p)
                PART_TitleBox.Text = p.DisplayName;
        }
    }

    private void OnTargetComboChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PART_TargetCombo.SelectedItem is PickerItem p)
        {
            if (string.IsNullOrWhiteSpace(PART_TitleBox.Text))
                PART_TitleBox.Text = p.DisplayName;
        }
    }

    private void OnBrowseClicked(object sender, RoutedEventArgs e)
    {
        if (PART_KindCombo.SelectedItem is not ComboBoxItem item) return;
        var kind = item.Tag as string;

        if (kind == "ExternalFile")
        {
            var dlg = new OpenFileDialog { Title = "اختر ملفًا" };
            if (dlg.ShowDialog() == true)
            {
                PART_TargetBox.Text = dlg.FileName;
                if (string.IsNullOrWhiteSpace(PART_TitleBox.Text))
                    PART_TitleBox.Text = System.IO.Path.GetFileName(dlg.FileName);
            }
        }
        else if (kind == "ExternalFolder")
        {
            var dlg = new OpenFolderDialog { Title = "اختر مجلدًا" };
            if (dlg.ShowDialog() == true)
            {
                PART_TargetBox.Text = dlg.FolderName;
                if (string.IsNullOrWhiteSpace(PART_TitleBox.Text))
                    PART_TitleBox.Text = System.IO.Path.GetFileName(dlg.FolderName);
            }
        }
    }

    private void OnAddClicked(object sender, RoutedEventArgs e)
    {
        if (PART_KindCombo.SelectedItem is not ComboBoxItem item) return;

        var kind = item.Tag as string ?? "Screen";
        var title = PART_TitleBox.Text?.Trim() ?? "";
        var folderId = (PART_FolderCombo.SelectedItem as NavItem)?.Id;

        if (string.IsNullOrEmpty(title))
        {
            MessageBox.Show("أدخل عنوانًا.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string target;
        if (kind == "Screen" || kind == "Report")
        {
            if (PART_TargetCombo.SelectedItem is not PickerItem p)
            {
                MessageBox.Show("اختر عنصرًا من القائمة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            target = p.Id;
        }
        else if (kind == "LocalFolder")
        {
            target = "";
        }
        else
        {
            target = PART_TargetBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("أدخل الهدف.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        Result = new AddItemDialogResult
        {
            Kind = kind,
            Title = title,
            Target = target,
            FolderId = string.IsNullOrEmpty(folderId) ? null : folderId
        };

        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public sealed class AddItemDialogResult
{
    public string Kind { get; set; } = "Screen";
    public string Title { get; set; } = "";
    public string Target { get; set; } = "";
    public string? FolderId { get; set; }
}

public sealed class PickerItem
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
