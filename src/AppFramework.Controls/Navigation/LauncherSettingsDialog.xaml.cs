using System;
using System.Windows;
using System.Windows.Controls;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Controls.Navigation;

public partial class LauncherSettingsDialog : Window
{
    private LauncherSettings _settings;

    public LauncherSettings? Result { get; private set; }

    public LauncherSettingsDialog(LauncherSettings current)
    {
        InitializeComponent();
        _settings = current?.Clone() ?? new LauncherSettings();

        LoadSettings(_settings);
    }

    // ==========================================================
    //  Load / Save
    // ==========================================================

    private void LoadSettings(LauncherSettings s)
    {
        PART_SizeSlider.Value = s.IconSize;
        PART_FontSlider.Value = s.LabelFontSize;
        PART_ShowLabelsCheck.IsChecked = s.ShowLabels;
        PART_ShowBadgesCheck.IsChecked = s.ShowBadges;
        PART_ShowUnseenBadgeCheck.IsChecked = s.ShowUnseenCountBadge;

        PART_ShowPinnedCheck.IsChecked = s.ShowPinnedGroup;
        PART_ShowFavoritesCheck.IsChecked = s.ShowFavoritesGroup;
        PART_ShowMostUsedCheck.IsChecked = s.ShowMostUsedGroup;
        PART_ShowRecentCheck.IsChecked = s.ShowRecentGroup;
        PART_ShowNewCheck.IsChecked = s.ShowNewGroup;
        PART_ShowAllScreensCheck.IsChecked = s.ShowAllScreensGroup;
        PART_ShowReportsCheck.IsChecked = s.ShowReportsGroup;
        PART_ShowExternalCheck.IsChecked = s.ShowExternalGroup;
        PART_ShowMyItemsCheck.IsChecked = s.ShowMyItemsGroup;
        PART_ShowPersonalTreeCheck.IsChecked = s.ShowPersonalTreeGroup;

        PART_AllowFavoritesCheck.IsChecked = s.AllowFavorites;
        PART_AllowPinningCheck.IsChecked = s.AllowPinning;
        PART_AllowHidingCheck.IsChecked = s.AllowHiding;
        PART_AllowDeletingCheck.IsChecked = s.AllowDeleting;
        PART_AllowEditingCheck.IsChecked = s.AllowEditing;
        PART_AllowAddingCheck.IsChecked = s.AllowAdding;
        PART_AllowDragCheck.IsChecked = s.EnableDragAndDrop;
        PART_ShowHistoryButtonCheck.IsChecked = s.ShowHistoryButton;

        SelectComboByTag(PART_ClickPolicyCombo, s.ClickPolicy.ToString());
        SelectComboByTag(PART_SortCombo, s.SortMode.ToString());

        UpdateSizeLabel();
        UpdateFontLabel();
    }

    private LauncherSettings BuildFromUi()
    {
        var s = _settings.Clone();

        s.IconSize = (int)PART_SizeSlider.Value;
        s.LabelFontSize = (int)PART_FontSlider.Value;
        s.ShowLabels = PART_ShowLabelsCheck.IsChecked ?? true;
        s.ShowBadges = PART_ShowBadgesCheck.IsChecked ?? true;
        s.ShowUnseenCountBadge = PART_ShowUnseenBadgeCheck.IsChecked ?? true;

        s.ShowPinnedGroup = PART_ShowPinnedCheck.IsChecked ?? true;
        s.ShowFavoritesGroup = PART_ShowFavoritesCheck.IsChecked ?? true;
        s.ShowMostUsedGroup = PART_ShowMostUsedCheck.IsChecked ?? true;
        s.ShowRecentGroup = PART_ShowRecentCheck.IsChecked ?? true;
        s.ShowNewGroup = PART_ShowNewCheck.IsChecked ?? true;
        s.ShowAllScreensGroup = PART_ShowAllScreensCheck.IsChecked ?? true;
        s.ShowReportsGroup = PART_ShowReportsCheck.IsChecked ?? true;
        s.ShowExternalGroup = PART_ShowExternalCheck.IsChecked ?? true;
        s.ShowMyItemsGroup = PART_ShowMyItemsCheck.IsChecked ?? true;
        s.ShowPersonalTreeGroup = PART_ShowPersonalTreeCheck.IsChecked ?? true;

        s.AllowFavorites = PART_AllowFavoritesCheck.IsChecked ?? true;
        s.AllowPinning = PART_AllowPinningCheck.IsChecked ?? true;
        s.AllowHiding = PART_AllowHidingCheck.IsChecked ?? true;
        s.AllowDeleting = PART_AllowDeletingCheck.IsChecked ?? true;
        s.AllowEditing = PART_AllowEditingCheck.IsChecked ?? true;
        s.AllowAdding = PART_AllowAddingCheck.IsChecked ?? true;
        s.EnableDragAndDrop = PART_AllowDragCheck.IsChecked ?? true;
        s.ShowHistoryButton = PART_ShowHistoryButtonCheck.IsChecked ?? true;

        if (PART_ClickPolicyCombo.SelectedItem is ComboBoxItem cp &&
            Enum.TryParse<ClickPolicy>(cp.Tag as string, out var policy))
            s.ClickPolicy = policy;

        if (PART_SortCombo.SelectedItem is ComboBoxItem sc &&
            Enum.TryParse<LauncherSortMode>(sc.Tag as string, out var sort))
            s.SortMode = sort;

        return s;
    }

    // ==========================================================
    //  UI Events
    // ==========================================================

    private static void SelectComboByTag(ComboBox combo, string tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && (item.Tag as string) == tag)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private void OnSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => UpdateSizeLabel();

    private void UpdateSizeLabel()
    {
        if (PART_SizeValue is null) return;
        PART_SizeValue.Text = ((int)PART_SizeSlider.Value).ToString();
    }

    private void OnFontChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        => UpdateFontLabel();

    private void UpdateFontLabel()
    {
        if (PART_FontValue is null) return;
        PART_FontValue.Text = ((int)PART_FontSlider.Value).ToString();
    }

    private void OnResetToDefaultsClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "هل تريد استعادة الإعدادات الافتراضية؟",
            "تأكيد",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        var defaults = new LauncherSettings();
        _settings = defaults;
        LoadSettings(defaults);
    }

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        Result = BuildFromUi();
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
