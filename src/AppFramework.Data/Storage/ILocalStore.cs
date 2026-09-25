using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Abstractions.Models.Data;

namespace AppFramework.Data.Storage;

/// <summary>
/// مخزن محلي للبيانات — يُستخدم لـ Offline-first + Outbox.
/// </summary>
public interface ILocalStore
{
    // ==========================================================
    //  Reads
    // ==========================================================

    /// <summary>قراءة صفحة من البيانات لمصدر معيّن.</summary>
    Task<PagedResult<object>> QueryAsync(DataRequest request, CancellationToken ct = default);

    /// <summary>قراءة المفاتيح فقط لمصدر معيّن.</summary>
    Task<PagedResult<ItemKey>> QueryKeysAsync(DataRequest request, CancellationToken ct = default);

    /// <summary>قراءة عنصر واحد بمفتاحه.</summary>
    Task<object?> GetItemAsync(string sourceKey, string itemKey, CancellationToken ct = default);

    // ==========================================================
    //  Writes
    // ==========================================================

    /// <summary>إدراج أو تحديث دفعة.</summary>
    Task UpsertBatchAsync(string sourceKey, IReadOnlyList<object> items, CancellationToken ct = default);

    /// <summary>حذف عنصر.</summary>
    Task RemoveAsync(string sourceKey, string itemKey, CancellationToken ct = default);

    // ==========================================================
    //  Outbox
    // ==========================================================

    /// <summary>إضافة تغيير إلى Outbox.</summary>
    Task EnqueueChangeAsync(DataChange change, CancellationToken ct = default);

    /// <summary>قراءة دفعة من الـ Outbox.</summary>
    Task<IReadOnlyList<DataChange>> DequeuePendingAsync(int batchSize, CancellationToken ct = default);

    /// <summary>حذف مدخلات Outbox بعد نجاح الإرسال.</summary>
    Task MarkSyncedAsync(IEnumerable<DataChange> changes, CancellationToken ct = default);

    /// <summary>تسجيل فشل إرسال (يزيد Attempts + يحدّث LastError).</summary>
    Task MarkFailedAsync(string sourceKey, string? itemKey, string error, CancellationToken ct = default);

    /// <summary>عدد المدخلات المعلّقة في Outbox.</summary>
    Task<int> GetPendingCountAsync(string? sourceKey = null, CancellationToken ct = default);
}