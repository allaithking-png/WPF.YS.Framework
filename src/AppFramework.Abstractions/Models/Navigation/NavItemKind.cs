namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// نوع عنصر في مستكشف التنقل.
/// </summary>
public enum NavItemKind
{
    /// <summary>مجلد تنظيمي.</summary>
    Folder,

    /// <summary>شاشة WPF.</summary>
    Screen,

    /// <summary>تقرير.</summary>
    Report,

    /// <summary>ملف على القرص.</summary>
    ExternalFile,

    /// <summary>مجلد على القرص.</summary>
    ExternalFolder,

    /// <summary>رابط إلكتروني.</summary>
    Url,

    /// <summary>نوع مخصص.</summary>
    Custom
}