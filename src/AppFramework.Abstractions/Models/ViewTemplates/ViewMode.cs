namespace AppFramework.Abstractions.Models.ViewTemplates;

/// <summary>
/// طريقة عرض البيانات.
/// </summary>
public enum ViewMode
{
    // ===== طرق عرض أساسية =====
    /// <summary>عرض شبكي (DataGrid).</summary>
    Grid,

    /// <summary>عرض قائمة (ListBox).</summary>
    List,

    /// <summary>عرض بطاقات.</summary>
    Card,

    // ===== طرق عرض إضافية =====
    /// <summary>عرض شجري (TreeView).</summary>
    Tree,

    /// <summary>خط زمني.</summary>
    Timeline,

    /// <summary>تقويم.</summary>
    Calendar,

    /// <summary>لوحة كانبان.</summary>
    Kanban,

    /// <summary>خلاصات (Feed).</summary>
    Feed,

    /// <summary>معرض صور.</summary>
    Gallery,

    /// <summary>خريطة.</summary>
    Map,

    /// <summary>رئيسي-تفاصيل.</summary>
    MasterDetail,

    /// <summary>طريقة عرض مخصصة.</summary>
    Custom
}