using System.Windows;
using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Controls.Menus;

/// <summary>
/// ظ…ط²ظˆظ‘ط¯ ط¹ط±ط¶ ط§ظ„ظ‚ظˆط§ط¦ظ… â€” ظٹط­ظˆظ‘ظ„ MenuDefinition ط¥ظ„ظ‰ ط¹ظ†طµط± WPF.
/// </summary>
/// <remarks>
/// ظƒظ„ ظ…ط²ظˆظ‘ط¯ ظ…ط³ط¤ظˆظ„ ط¹ظ† ظ†ظˆط¹ ظˆط§ط­ط¯ ظ…ظ† ط§ظ„ط¹ط±ظˆط¶ (MenuBar, Toolbar, Ribbon, ...).
/// </remarks>
public interface IMenuProvider
{
    /// <summary>ظ†ظˆط¹ ط§ظ„ط¹ط±ط¶ ط§ظ„ط°ظٹ ظٹط¯ط¹ظ…ظ‡ ظ‡ط°ط§ ط§ظ„ظ…ط²ظˆظ‘ط¯.</summary>
    MenuHost Host { get; }

    /// <summary>
    /// ط¨ظ†ط§ط، ط¹ظ†طµط± WPF ظ…ظ† طھط¹ط±ظٹظپ ظ‚ط§ط¦ظ…ط©.
    /// </summary>
    FrameworkElement Build(MenuDefinition definition);
}
