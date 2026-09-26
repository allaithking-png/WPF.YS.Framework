using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AppFramework.Sync.Connectivity;

/// <summary>
/// تنفيذ <see cref="IConnectivityMonitor"/> باستخدام
/// <see cref="NetworkInterface.GetIsNetworkAvailable"/> + مراقبة تغييرات الشبكة.
/// </summary>
public sealed class NetworkConnectivityMonitor : IConnectivityMonitor, IDisposable
{
    private readonly ILogger<NetworkConnectivityMonitor> _logger;
    private bool _isOnline;
    private bool _isMonitoring;
    private readonly object _lock = new();

    public NetworkConnectivityMonitor(ILogger<NetworkConnectivityMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isOnline = NetworkInterface.GetIsNetworkAvailable();
    }

    public bool IsOnline
    {
        get => _isOnline;
        private set
        {
            if (_isOnline == value) return;
            _isOnline = value;
            _logger.LogInformation("Connectivity changed: {Status}", value ? "Online" : "Offline");
            ConnectivityChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? ConnectivityChanged;

    public Task<bool> CheckAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var available = NetworkInterface.GetIsNetworkAvailable();
            IsOnline = available;
            return Task.FromResult(available);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check connectivity");
            IsOnline = false;
            return Task.FromResult(false);
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isMonitoring) return;
            _isMonitoring = true;

            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;

            _logger.LogDebug("Connectivity monitor started");
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isMonitoring) return;
            _isMonitoring = false;

            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;

            _logger.LogDebug("Connectivity monitor stopped");
        }
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        IsOnline = e.IsAvailable;
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        // قد تتغير الحالة (مثلًا: كبل شبكة فُصل)
        _ = CheckAsync();
    }

    public void Dispose() => Stop();
}