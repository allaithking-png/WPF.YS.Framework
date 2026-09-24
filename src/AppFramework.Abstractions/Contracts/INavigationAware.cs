using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Navigation;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة العنصر على الاستجابة لأحداث التنقل.
/// تُستخدم عادة من الـ Shell أو الصفحات الرئيسية.
/// </summary>
public interface INavigationAware : IAppAware
{
    /// <summary>يُستدعى قبل الانتقال. أرجع false لإلغاء التنقل.</summary>
    Task<bool> OnNavigatingFromAsync(NavigationContext context, CancellationToken ct = default);

    /// <summary>يُستدعى بعد إتمام الانتقال.</summary>
    Task OnNavigatedToAsync(NavigationContext context, CancellationToken ct = default);
}
