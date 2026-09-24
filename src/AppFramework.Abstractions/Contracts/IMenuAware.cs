using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة الشاشة على بناء قائمتها الخاصة (Ribbon / MenuBar / Toolbar).
/// </summary>
public interface IMenuAware : IAppAware
{
    /// <summary>
    /// يُستدعى من <c>IMenuManager</c> عند تفعيل الشاشة
    /// لبناء القائمة المناسبة للسياق الحالي.
    /// </summary>
    MenuDefinition BuildMenu(MenuContext context);
}
