using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Data;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// الخدمة المركزية للتعامل مع البيانات (Offline-first).
/// </summary>
public interface IDataService
{
    /// <summary>تسجيل مصدر بيانات.</summary>
    void RegisterSource(IDataSource source);

    /// <summary>الحصول على مصدر بيانات (يرمي استثناء إن لم يكن مسجلًا).</summary>
    IDataSource GetSource(string key);

    /// <summary>محاولة الحصول على مصدر بيانات.</summary>
    bool TryGetSource(string key, out IDataSource? source);

    /// <summary>جلب دفعة كاملة.</summary>
    Task<PagedResult<object>> LoadBatchAsync(DataRequest request, CancellationToken ct = default);

    /// <summary>جلب المفاتيح فقط (سريع).</summary>
    Task<PagedResult<ItemKey>> LoadKeysAsync(DataRequest request, CancellationToken ct = default);

    /// <summary>جلب عنصر منفرد.</summary>
    Task<object?> LoadItemAsync(string sourceKey, string itemKey, CancellationToken ct = default);

    /// <summary>حفظ عنصر.</summary>
    Task SaveItemAsync(string sourceKey, object item, CancellationToken ct = default);

    /// <summary>حفظ دفعة.</summary>
    Task SaveBatchAsync(string sourceKey, IReadOnlyList<object> items, CancellationToken ct = default);

    /// <summary>حذف عنصر.</summary>
    Task DeleteAsync(string sourceKey, string itemKey, CancellationToken ct = default);

    /// <summary>الاشتراك في تغييرات مصدر معيّن.</summary>
    IDisposable Subscribe(string sourceKey, Func<DataChange, Task> handler);

    /// <summary>نشر تغيير يدويًا (بعد حفظ ناجح).</summary>
    void PublishChange(DataChange change);

    /// <summary>جلب البيانات محليًا أولًا (Offline-first).</summary>
    Task<PagedResult<object>> LoadBatchLocalFirstAsync(DataRequest request, CancellationToken ct = default);
}