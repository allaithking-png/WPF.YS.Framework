namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// نوع حدث في سجل التغييرات.
/// </summary>
public enum HistoryKind
{
    /// <summary>عنصر جديد أُضيف.</summary>
    Created,

    /// <summary>عنصر موجود تم تعديله.</summary>
    Updated,

    /// <summary>عنصر حُذف.</summary>
    Deleted,

    /// <summary>عنصر نُقل/أُعيد ترتيبه.</summary>
    Moved,

    /// <summary>عنصر أُضيف إلى مجلد.</summary>
    AddedToFolder,

    /// <summary>عنصر أُزيل من مجلد.</summary>
    RemovedFromFolder,

    /// <summary>تغيير في الإعدادات.</summary>
    SettingsChanged,

    /// <summary>حدث آخر.</summary>
    Other
}