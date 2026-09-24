using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Data;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// مزوّد بيانات (REST, OData, XPO, SQLite, ...).
/// </summary>
public interface IDataSource
{
    /// <summary>مفتاح المصدر (فريد).</summary>
    string Key { get; }

    /// <summary>جلب المفاتيح فقط (سريع).</summary>
    Task<PagedResult<ItemKey>> GetKeysAsync(DataRequest request, CancellationToken ct = default);

    /// <summary>جلب عنصر منفرد.</summary>
    Task<object?> GetItemAsync(string itemKey, DataRequest request, CancellationToken ct = default);

    /// <summary>جلب دفعة كاملة.</summary>
    Task<PagedResult<object>> GetBatchAsync(DataRequest request, CancellationToken ct = default);

    /// <summary>حفظ عنصر منفرد.</summary>
    Task SaveItemAsync(object item, CancellationToken ct = default);

    /// <summary>حفظ دفعة.</summary>
    Task SaveBatchAsync(IReadOnlyList<object> items, CancellationToken ct = default);

    /// <summary>حذف عنصر.</summary>
    Task DeleteAsync(string itemKey, CancellationToken ct = default);
}