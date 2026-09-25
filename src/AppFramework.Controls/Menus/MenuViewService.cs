using System;
using AppFramework.Abstractions.Models.Menu;
using AppFramework.Abstractions.Services;

namespace AppFramework.Controls.Menus;

/// <summary>
/// خدمة مساعدة للتفاعل مع قوائم العرض.
/// </summary>
public sealed class MenuViewService
{
    private readonly IMenuManager _menuManager;
    private readonly MenuProviderRegistry _registry;

    public MenuViewService(IMenuManager menuManager, MenuProviderRegistry registry)
    {
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public IMenuManager Manager => _menuManager;
    public MenuProviderRegistry Registry => _registry;

    /// <summary>تسجيل مزوّد جديد.</summary>
    public void RegisterProvider(IMenuProvider provider) => _registry.Register(provider);

    /// <summary>تبديل نوع العرض.</summary>
    public void SetHost(MenuHost host) => _menuManager.CurrentHost = host;

    /// <summary>إعادة بناء القائمة الحالية.</summary>
    public void Refresh() => _menuManager.Refresh();
}