using System.Windows;
using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Controls.Menus;

/// <summary>
/// ظ…ط²ظˆظ‘ط¯ ط±ظٹط¨ظˆظ† â€” ظٹط³طھط®ط¯ظ… ط­ط§ظ„ظٹظ‹ط§ ط´ط±ظٹط· ط£ط¯ظˆط§طھ ظƒط¨ط¯ظٹظ„.
/// ط³ظٹظڈط³طھط¨ط¯ظ„ ط¨ظ€ Ribbon ط­ظ‚ظٹظ‚ظٹ ظپظٹ PR ظ„ط§ط­ظ‚ظ‹ط§ (Fluent.Ribbon ط£ظˆ DevExpress).
/// </summary>
public sealed class RibbonProvider : IMenuProvider
{
    private readonly ToolbarProvider _fallback = new();

    public MenuHost Host => MenuHost.Ribbon;

    public FrameworkElement Build(MenuDefinition definition)
    {
        // fallback ظ…ط¤ظ‚طھظ‹ط§
        return _fallback.Build(definition);
    }
}
