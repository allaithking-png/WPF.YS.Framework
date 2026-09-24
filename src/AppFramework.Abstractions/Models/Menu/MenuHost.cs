namespace AppFramework.Abstractions.Models.Menu;

/// <summary>
/// مكان عرض القائمة.
/// </summary>
public enum MenuHost
{
    /// <summary>شريط Ribbon علوي.</summary>
    Ribbon,

    /// <summary>شريط قوائم كلاسيكي (MenuBar).</summary>
    MenuBar,

    /// <summary>شريط أدوات (Toolbar).</summary>
    Toolbar,

    /// <summary>قائمة سياقية (ContextMenu).</summary>
    ContextMenu,

    /// <summary>مستكشف (Explorer Tree).</summary>
    Explorer
}