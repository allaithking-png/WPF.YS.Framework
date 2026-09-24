using System.Threading;
using System.Threading.Tasks;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة عنصر بيانات واحد (صف، بطاقة، عقدة) على جلب بياناته وحفظها بشكل مستقل.
/// </summary>
/// <remarks>
/// هذا هو جوهر نموذج "كل عنصر يجلب بياناته":
/// - عند سحب عنصر إلى الواجهة، تُمرَّر المفاتيح الأساسية فقط.
/// - كل عنصر يجلب بياناته الكاملة عند الحاجة (Lazy Loading).
/// </remarks>
public interface IDataItemAware : IAppAware
{
    /// <summary>مفتاح مصدر البيانات الخاص بهذا العنصر.</summary>
    string? ItemDataSourceKey { get; }

    /// <summary>المفتاح الفريد لهذا العنصر داخل المصدر.</summary>
    string ItemKey { get; }

    /// <summary>جلب بيانات هذا العنصر فقط.</summary>
    Task LoadSelfAsync(CancellationToken ct = default);

    /// <summary>حفظ تعديلات هذا العنصر فقط.</summary>
    Task SaveSelfAsync(CancellationToken ct = default);

    /// <summary>هل يحتوي العنصر على تعديلات غير محفوظة؟</summary>
    bool IsDirty { get; }
}
