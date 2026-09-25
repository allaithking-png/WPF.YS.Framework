using System.Threading;
using System.Threading.Tasks;

namespace AppFramework.Core.Navigation;

/// <summary>
/// خدمة حفظ واسترجاع جلسة التنقل.
/// </summary>
public interface INavigationSessionService
{
    /// <summary>حفظ الجلسة الحالية.</summary>
    Task SaveAsync(NavigationSession session, CancellationToken ct = default);

    /// <summary>استرجاع جلسة مستخدم.</summary>
    Task<NavigationSession?> LoadAsync(string userId, CancellationToken ct = default);

    /// <summary>حذف جلسة مستخدم.</summary>
    Task DeleteAsync(string userId, CancellationToken ct = default);
}