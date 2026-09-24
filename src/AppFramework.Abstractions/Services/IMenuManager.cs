using System;
using System.Windows.Input;
using AppFramework.Abstractions.Models.Menu;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// مدير القوائم (Ribbon / MenuBar / Toolbar).
/// </summary>
public interface IMenuManager
{
    /// <summary>تسجيل شاشة مع دالة بناء قائمتها.</summary>
    void RegisterScreen(string screenId, Func<MenuContext, MenuDefinition> factory);

    /// <summary>تسجيل أمر برمجي عام.</summary>
    void RegisterCommand(string key, ICommand command);

    /// <summary>حلّ أمر عبر مفتاحه.</summary>
    ICommand? ResolveCommand(string key);

    /// <summary>تفعيل قائمة شاشة.</summary>
    void ActivateScreen(string screenId, object? viewModel = null);

    /// <summary>إلغاء تفعيل شاشة.</summary>
    void DeactivateScreen(string screenId);

    /// <summary>إعادة بناء القائمة الحالية.</summary>
    void Refresh();

    /// <summary>القائمة الحالية.</summary>
    MenuDefinition? Current { get; }

    /// <summary>مكان العرض الحالي.</summary>
    MenuHost CurrentHost { get; set; }

    /// <summary>حدث عند تغيير القائمة.</summary>
    event EventHandler<MenuDefinition>? MenuChanged;
}