using System.Threading;
using System.Threading.Tasks;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة العنصر على التعامل مع مصدر بيانات.
/// أي شاشة تعرض أو تعدّل بيانات تنفّذ هذه الواجهة.
/// </summary>
public interface IDataAware : IAppAware
{
    /// <summary>
    /// مفتاح مصدر البيانات الأساسي (مثال: "Orders").
    /// إذا كان null، فالشاشة لا تعتمد على مصدر أساسي (قد تعتمد على مصادر فرعية فقط).
    /// </summary>
    string? DataSourceKey { get; }

    /// <summary>تحميل الدفعة الأولى من البيانات.</summary>
    Task LoadInitialAsync(CancellationToken ct = default);

    /// <summary>تحميل دفعة إضافية (Paging / Infinite Scroll).</summary>
    Task LoadMoreAsync(CancellationToken ct = default);

    /// <summary>حفظ كل التغييرات المعلّقة.</summary>
    Task SaveAsync(CancellationToken ct = default);
}
