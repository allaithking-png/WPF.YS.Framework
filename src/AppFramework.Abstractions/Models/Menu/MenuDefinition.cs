using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Menu;

/// <summary>
/// تعريف قائمة كاملة تُبنى لشاشة معيّنة.
/// </summary>
public sealed class MenuDefinition
{
    /// <summary>مكان العرض المفضّل.</summary>
    public MenuHost Host { get; set; } = MenuHost.MenuBar;

    /// <summary>عناصر القائمة.</summary>
    public List<MenuItemDescriptor> Items { get; set; } = new();
}