namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// إعدادات سطح المكتب (Android-style launcher).
/// </summary>
public sealed class LauncherSettings
{
    // ==========================================================
    //  Behavior — Toggles
    // ==========================================================

    /// <summary>السماح بالتفضيل (Add to Favorites).</summary>
    public bool AllowFavorites { get; set; } = true;

    /// <summary>السماح بالتثبيت (Pin).</summary>
    public bool AllowPinning { get; set; } = true;

    /// <summary>السماح بالإخفاء (Hide).</summary>
    public bool AllowHiding { get; set; } = true;

    /// <summary>السماح بالحذف (Delete).</summary>
    public bool AllowDeleting { get; set; } = true;

    /// <summary>السماح بالتعديل (Edit).</summary>
    public bool AllowEditing { get; set; } = true;

    /// <summary>السماح بالإضافة اليدوية.</summary>
    public bool AllowAdding { get; set; } = true;

    /// <summary>السماح بالسحب والإفلات.</summary>
    public bool AllowDragAndDrop { get; set; } = true;

    /// <summary>عرض زر "سجل التغييرات".</summary>
    public bool ShowHistoryButton { get; set; } = true;

    /// <summary>عرض شارات "جديد" / "محدّث".</summary>
    public bool ShowBadges { get; set; } = true;

    /// <summary>عرض شارة عدد الأحداث غير المشاهدة.</summary>
    public bool ShowUnseenCountBadge { get; set; } = true;
    // ==========================================================
    //  Appearance
    // ==========================================================

    /// <summary>حجم الأيقونة بالبكسل (48 = صغير، 96 = متوسط، 128 = كبير).</summary>
    public double IconSize { get; set; } = 96;

    /// <summary>إظهار أسماء العناصر.</summary>
    public bool ShowLabels { get; set; } = true;

    /// <summary>حجم الخط للاسم.</summary>
    public double LabelFontSize { get; set; } = 12;

    /// <summary>المسافة بين الأيقونات (px).</summary>
    public double Spacing { get; set; } = 8;

    /// <summary>حجم المجموعة (عدد الأعمدة في WrapPanel يُحسب تلقائيًا).</summary>
    public int GroupColumns { get; set; } = 8;

    // ==========================================================
    //  Behavior
    // ==========================================================

    /// <summary>سياسة النقر.</summary>
    public ClickPolicy ClickPolicy { get; set; } = ClickPolicy.DoubleClickToOpen;

    /// <summary>تفعيل السحب والإفلات.</summary>
    public bool EnableDragAndDrop { get; set; } = true;

    /// <summary>مدة Long-press (بالملّي ثانية).</summary>
    public int LongPressMs { get; set; } = 500;

    /// <summary>وضع الترتيب.</summary>
    public LauncherSortMode SortMode { get; set; } = LauncherSortMode.Custom;

    /// <summary>عرض المجموعات (Pinned, Favorites, ...).</summary>
    public bool ShowPinnedGroup { get; set; } = true;
    public bool ShowFavoritesGroup { get; set; } = true;
    public bool ShowMostUsedGroup { get; set; } = true;
    public bool ShowRecentGroup { get; set; } = true;
    public bool ShowNewGroup { get; set; } = true;
    public bool ShowAllScreensGroup { get; set; } = true;
    public bool ShowReportsGroup { get; set; } = true;
    public bool ShowExternalGroup { get; set; } = true;
    public bool ShowMyItemsGroup { get; set; } = true;
    public bool ShowPersonalTreeGroup { get; set; } = true;
    

    // ==========================================================
    //  Helpers
    // ==========================================================

    /// <summary>ينسخ الإعدادات (لتعديل آمن).</summary>
    public LauncherSettings Clone() => (LauncherSettings)MemberwiseClone();
}

/// <summary>سياسة النقر.</summary>
public enum ClickPolicy
{
    /// <summary>نقر واحد = فتح (Android).</summary>
    SingleClickToOpen,

    /// <summary>نقر مزدوج = فتح، نقر واحد = تحديد (Windows).</summary>
    DoubleClickToOpen,

    /// <summary>نقر واحد = تحديد فقط (اختيار قبل العمل).</summary>
    SelectOnly
}

/// <summary>وضع الترتيب.</summary>
public enum LauncherSortMode
{
    /// <summary>ترتيب يدوي (بالسحب).</summary>
    Custom,

    /// <summary>أبجدي.</summary>
    Name,

    /// <summary>الأكثر استخدامًا.</summary>
    MostUsed,

    /// <summary>الأحدث.</summary>
    RecentlyUsed
}
