using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Contracts;      // ← أضف هذا السطر
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Core.Base;

/// <summary>
/// أساس اختياري للنافذة الرئيسية (Shell).
/// </summary>
public abstract class BaseMainForm : BaseScreen
{
    /// <summary>اسم المنطقة الرئيسية للمحتوى.</summary>
    public virtual string MainContentRegion => "MainContentRegion";

    /// <summary>اسم منطقة الشريط الجانبي.</summary>
    public virtual string SidebarRegion => "SidebarRegion";

    /// <summary>اسم منطقة شريط الأدوات.</summary>
    public virtual string ToolbarRegion => "ToolbarRegion";

    public override Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default)
    {
        OnShellReady();
        return Task.CompletedTask;
    }

    /// <summary>نقطة تمديد بعد جهوزية الـ Shell.</summary>
    protected virtual void OnShellReady() { }
}