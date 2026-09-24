using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// خدمة المزامنة بين المحلي والبعيد.
/// </summary>
public interface ISyncService
{
    /// <summary>هل الاتصال بالشبكة متاح؟</summary>
    bool IsOnline { get; }

    /// <summary>مزامنة مصدر واحد.</summary>
    Task<SyncResult> SyncAsync(string sourceKey, CancellationToken ct = default);

    /// <summary>مزامنة كل المصادر.</summary>
    Task<SyncResult> SyncAllAsync(CancellationToken ct = default);

    /// <summary>بدء المزامنة التلقائية.</summary>
    void StartAutoSync(TimeSpan interval);

    /// <summary>إيقاف المزامنة التلقائية.</summary>
    void StopAutoSync();

    /// <summary>حدث عند انتهاء مزامنة.</summary>
    event EventHandler<SyncResult>? SyncCompleted;

    /// <summary>حدث عند تغيير حالة الاتصال.</summary>
    event EventHandler<bool>? ConnectivityChanged;
}

/// <summary>نتيجة مزامنة.</summary>
public sealed record SyncResult(
    string SourceKey,
    int Pushed,
    int Pulled,
    int Conflicts,
    Exception? Error = null);