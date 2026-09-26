using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Services;

namespace AppFramework.Controls.Notifications;

/// <summary>
/// تنفيذ <see cref="INotificationService"/> بـ WPF Toast.
/// </summary>
public sealed class ToastNotificationService : INotificationService
{
    private readonly ILogger<ToastNotificationService> _logger;
    private ToastHost? _host;

    public ToastNotificationService(ILogger<ToastNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<NotificationActionEventArgs>? ActionInvoked;

    // ==========================================================
    //  Simple methods
    // ==========================================================

    public void Info(string title, string message)
        => Show(title, message, ToastSeverity.Info);

    public void Success(string title, string message)
        => Show(title, message, ToastSeverity.Success);

    public void Warning(string title, string message)
        => Show(title, message, ToastSeverity.Warning);

    public void Error(string title, string message)
        => Show(title, message, ToastSeverity.Error);

    // ==========================================================
    //  With actions
    // ==========================================================

    public void ShowWithActions(string title, string message, params NotificationAction[] actions)
    {
        EnsureHost();
        _host!.ShowToast(title, message, ToastSeverity.Info, null, actions, OnActionClicked);
    }

    // ==========================================================
    //  Internal
    // ==========================================================

    private void Show(string title, string message, ToastSeverity severity)
    {
        EnsureHost();
        _host!.ShowToast(title, message, severity, null, null, null);
        _logger.LogDebug("Toast shown: [{Severity}] {Title}", severity, title);
    }

    private void EnsureHost()
    {
        if (_host is not null && _host.IsLoaded) return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            _logger.LogWarning("Application.Current is null — cannot show toast");
            return;
        }

        dispatcher.Invoke(() =>
        {
            _host ??= new ToastHost();
            if (!_host.IsVisible)
                _host.Show();
        });
    }

    private void OnActionClicked(NotificationAction action)
    {
        try
        {
            action.Callback?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Toast action callback threw");
        }

        ActionInvoked?.Invoke(this, new NotificationActionEventArgs("", action));
    }
}