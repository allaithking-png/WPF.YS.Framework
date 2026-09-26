using System;
using System.Threading;
using System.Threading.Tasks;
using AppFramework.Sync.Connectivity;

namespace AppFramework.Sync.Tests.TestHelpers;

/// <summary>
/// مراقب اتصال وهمي للاختبارات.
/// </summary>
public sealed class FakeConnectivityMonitor : IConnectivityMonitor
{
    private bool _isOnline;

    public FakeConnectivityMonitor(bool startOnline = true)
    {
        _isOnline = startOnline;
    }

    public bool IsOnline => _isOnline;

    public event EventHandler<bool>? ConnectivityChanged;

    public Task<bool> CheckAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_isOnline);
    }

    public void Start() { }
    public void Stop() { }

    /// <summary>محاكاة تغيير حالة الاتصال.</summary>
    public void SetOnline(bool online)
    {
        if (_isOnline == online) return;
        _isOnline = online;
        ConnectivityChanged?.Invoke(this, online);
    }
}