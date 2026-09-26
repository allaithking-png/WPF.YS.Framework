using System;
using System.Threading;
using System.Threading.Tasks;

namespace AppFramework.Sync.Connectivity;

/// <summary>
/// مراقب حالة الاتصال بالشبكة/الإنترنت.
/// </summary>
public interface IConnectivityMonitor
{
    /// <summary>هل الاتصال متاح حاليًا؟</summary>
    bool IsOnline { get; }

    /// <summary>حدث يُطلَق عند تغيير حالة الاتصال.</summary>
    event EventHandler<bool>? ConnectivityChanged;

    /// <summary>فحص الاتصال يدويًا.</summary>
    Task<bool> CheckAsync(CancellationToken ct = default);

    /// <summary>بدء المراقبة الدورية.</summary>
    void Start();

    /// <summary>إيقاف المراقبة.</summary>
    void Stop();
}