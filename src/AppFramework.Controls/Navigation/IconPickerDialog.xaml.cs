using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AppFramework.Controls.Navigation;

public partial class IconPickerDialog : Window
{
    public string? Result { get; private set; }

    private string _currentCategory = "General";
    private string _searchQuery = "";

    // ==========================================================
    //  Icon Catalog
    // ==========================================================

    private static readonly Dictionary<string, string[]> IconCatalog = new()
    {
        ["General"] = new[]
        {
            "📄", "📁", "📂", "📊", "📈", "📉", "📋", "📌", "📎", "📏",
            "🖥️", "💻", "⌨️", "🖱️", "🖨️", "📱", "☎️", "📞", "📟", "📠",
            "⭐", "✨", "🌟", "💫", "⚡", "🔥", "💡", "🎯", "🎨", "🎭",
            "✅", "❌", "⚠️", "ℹ️", "❓", "❗", "🔔", "🔕", "🔒", "🔓",
            "🔑", "🗝️", "🛡️", "🔨", "🪓", "🔧", "🔩", "⚙️", "🧰", "🧲"
        },
        ["Sales"] = new[]
        {
            "🛒", "🛍️", "💰", "💵", "💴", "💶", "💷", "💸", "💳", "🧾",
            "📦", "📫", "📬", "📭", "📮", "🏷️", "🎁", "🎀", "🎊", "🎉",
            "📊", "📈", "📉", "🏪", "🏬", "🏢", "🏦", "💼", "📅", "🗓️"
        },
        ["People"] = new[]
        {
            "👤", "👥", "👨", "👩", "🧑", "👦", "👧", "🧒", "👴", "👵",
            "👨‍💼", "👩‍💼", "👨‍💻", "👩‍💻", "👨‍🔧", "👩‍🔧", "🤝", "🙋", "🙋‍♂️", "🙋‍♀️",
            "👋", "👍", "👎", "👏", "🙌", "🤲", "💪", "🧠", "❤️", "💙"
        },
        ["Communication"] = new[]
        {
            "✉️", "📧", "📨", "📩", "📤", "📥", "📦", "📫", "📪", "📬",
            "📭", "📮", "📯", "📢", "📣", "🔔", "🔕", "💬", "💭", "🗨️",
            "🗯️", "📱", "☎️", "📞", "📟", "📠", "📡", "📺", "📻", "🎙️"
        },
        ["Files"] = new[]
        {
            "📄", "📃", "📑", "📊", "📈", "📉", "📋", "📁", "📂", "🗂️",
            "🗃️", "🗄️", "📰", "📓", "📔", "📕", "📗", "📘", "📙", "📚",
            "📖", "🔖", "🏷️", "✂️", "📎", "📌", "📍", "🖇️", "📐", "📏"
        },
        ["Media"] = new[]
        {
            "🎵", "🎶", "🎼", "🎤", "🎧", "🎷", "🎸", "🎹", "🎺", "🎻",
            "📻", "🎬", "🎥", "📹", "📷", "📸", "📼", "📺", "🎞️", "🎮",
            "🕹️", "🎲", "🎯", "🎳", "🎰", "🧩", "🎪", "🎨", "🎭", "🎬"
        },
        ["Time"] = new[]
        {
            "🕐", "🕑", "🕒", "🕓", "🕔", "🕕", "🕖", "🕗", "🕘", "🕙",
            "📅", "📆", "🗓️", "⏰", "⏱️", "⏲️", "⌚", "⌛", "⏳", "🌅",
            "🌄", "🌇", "🌆", "🌃", "🌉", "🌌", "🌠", "🎆", "🎇", "✨"
        },
        ["Nature"] = new[]
        {
            "🌍", "🌎", "🌏", "🌐", "🗺️", "🧭", "⛰️", "🏔️", "🌋", "🗻",
            "🏕️", "🏖️", "🏝️", "🏜️", "🌲", "🌳", "🌴", "🌵", "🌾", "🌿",
            "☘️", "🍀", "🎍", "🌺", "🌻", "🌹", "🌷", "🌼", "🌸", "💐"
        }
    };

    private static readonly Dictionary<string, string> CategoryNames = new()
    {
        ["General"] = "عام",
        ["Sales"] = "المبيعات",
        ["People"] = "الأشخاص",
        ["Communication"] = "التواصل",
        ["Files"] = "الملفات",
        ["Media"] = "الوسائط",
        ["Time"] = "الوقت",
        ["Nature"] = "الطبيعة"
    };

    // ==========================================================
    //  Construction
    // ==========================================================

    public IconPickerDialog(string? currentIcon = null)
    {
        InitializeComponent();

        BuildCategories();
        PopulateIcons();

        if (!string.IsNullOrEmpty(currentIcon))
            PART_ManualBox.Text = currentIcon;
    }

    private void BuildCategories()
    {
        var cats = IconCatalog.Keys.Select(k => new CategoryVm
        {
            Id = k,
            Name = CategoryNames.GetValueOrDefault(k, k)
        }).ToList();

        PART_Categories.ItemsSource = cats;
    }

    private void PopulateIcons()
    {
        IEnumerable<string> icons;

        if (!string.IsNullOrEmpty(_searchQuery))
        {
            icons = IconCatalog.Values
                .SelectMany(v => v)
                .Distinct();
        }
        else
        {
            icons = IconCatalog.TryGetValue(_currentCategory, out var list)
                ? list
                : IconCatalog["General"];
        }

        PART_Icons.ItemsSource = new ObservableCollection<string>(icons);
    }

    // ==========================================================
    //  Events
    // ==========================================================

    private void OnCategoryClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not string categoryId) return;

        _currentCategory = categoryId;
        _searchQuery = "";
        PART_SearchBox.Text = "";

        PopulateIcons();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = PART_SearchBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(_searchQuery))
        {
            PopulateIcons();
            return;
        }

        // ابحث في كل التصنيفات
        var all = IconCatalog.Values.SelectMany(v => v).Distinct();

        // بحث بسيط: كل الأيقونات تحتوي على البحث
        // (لكن الإيموجي ليس له اسم — نُظهر الكل)
        PopulateIcons();
    }

    private void OnIconClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not string icon) return;

        Result = icon;
        DialogResult = true;
        Close();
    }

    private void OnPickClicked(object sender, RoutedEventArgs e)
    {
        var manual = PART_ManualBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(manual))
        {
            MessageBox.Show("اختر أيقونة أو أدخلها يدويًا.", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = manual;
        DialogResult = true;
        Close();
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public sealed class CategoryVm
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}