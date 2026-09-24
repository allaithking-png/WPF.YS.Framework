using System.Threading;
using System.Threading.Tasks;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// دورة حياة الشاشة. أي عنصر يُعرَض كشاشة رئيسية أو فرعية ينفّذ هذه الواجهة.
/// </summary>
/// <remarks>
/// ملاحظة: استخدمنا <see cref="System.Threading.Tasks.Task"/> بدل <c>void</c>
/// في كل الأحداث لأن الشاشة قد تحتاج عملًا غير متزامن (تحميل، تحقق، حفظ).
/// </remarks>
public interface IScreenAware : IAppAware
{
    /// <summary>المعرّف الفريد للشاشة (يُستخدم في Navigation و Menu).</summary>
    string ScreenId { get; }

    /// <summary>العنوان المعروض للمستخدم.</summary>
    string ScreenTitle { get; }

    /// <summary>
    /// يُستدعى عند تنشيط الشاشة (بعد فتحها أو عند العودة إليها من تبويب آخر).
    /// هنا يتم تحميل البيانات الأولية.
    /// </summary>
    Task OnActivatedAsync(ScreenActivationContext context, CancellationToken ct = default);

    /// <summary>
    /// يُستدعى عند تعطيل الشاشة (عند إغلاقها أو الانتقال لشاشة أخرى).
    /// هنا يتم حفظ الحالة إن لزم.
    /// </summary>
    Task OnDeactivatedAsync(CancellationToken ct = default);

    /// <summary>
    /// يُستدعى قبل الإغلاق. أرجع <c>false</c> لإلغاء الإغلاق
    /// (مثلًا: "لديك تغييرات غير محفوظة، هل تريد الخروج؟").
    /// </summary>
    Task<bool> OnClosingAsync(CancellationToken ct = default);
}

/// <summary>سياق تنشيط الشاشة.</summary>
/// <param name="TargetLocation">موقع داخلي اختياري (سطر محدد، تبويب فرعي، ...).</param>
/// <param name="Parameters">بارامترات مرّرها الـ Navigation.</param>
/// <param name="IsRestored">هل هذه إعادة فتح من جلسة سابقة؟</param>
public sealed record ScreenActivationContext(
    string? TargetLocation = null,
    IReadOnlyDictionary<string, object?>? Parameters = null,
    bool IsRestored = false);
