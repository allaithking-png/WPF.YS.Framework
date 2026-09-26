using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using AppFramework.Abstractions.Services;

namespace AppFramework.Controls.Notifications;

/// <summary>
/// نافذة شفافة تعرض كل الإشعارات في الزاوية العلوية اليمنى.
/// </summary>
public partial class ToastHost : Window
{
    private readonly ILogger<ToastHost>? _logger;
    private readonly List<ToastControl> _activeToasts = new();

    public ToastHost(ILogger<ToastHost>? logger = null)
    {
        _logger = logger;
        InitializeComponent();
        PositionWindow();
    }

    // ==========================================================
    //  Show Toast
    // ==========================================================

    public void ShowToast(
    string title,
    string message,
    ToastSeverity severity,
    TimeSpan? duration = null,
    IReadOnlyList<NotificationAction>? actions = null,
    Action<NotificationAction>? onActionInvoked = null)
    {
        // ✅ تأكد أن النافذة مرئية
        if (!IsVisible)
            Show();

        // ✅ أعِد الظهور
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;

        var toast = new ToastControl();
        toast.Configure(title, message, severity, actions, onActionInvoked);

        toast.Closed += (_, _) => RemoveToast(toast);
        toast.Opacity = 0;
        _activeToasts.Add(toast);
        PART_Container.Children.Insert(0, toast);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
        toast.BeginAnimation(OpacityProperty, fadeIn);

        var dismiss = duration ?? GetDefaultDuration(severity);
        if (dismiss > TimeSpan.Zero)
        {
            var timer = new DispatcherTimer { Interval = dismiss };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                RemoveToast(toast);
            };
            timer.Start();
        }

        UpdateLayout();
        UpdatePosition();
    }

    // ==========================================================
    //  Internal
    // ==========================================================

    private void RemoveToast(ToastControl toast)
    {
        if (!_activeToasts.Contains(toast)) return;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        fadeOut.Completed += (_, _) =>
        {
            _activeToasts.Remove(toast);
            PART_Container.Children.Remove(toast);

            if (_activeToasts.Count == 0)
            {
                // ✅ أخفِ بـ Opacity بدل Hide
                var winFade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
                BeginAnimation(OpacityProperty, winFade);
            }
        };
        toast.BeginAnimation(OpacityProperty, fadeOut);
    }

    private static TimeSpan GetDefaultDuration(ToastSeverity severity)
        => severity switch
        {
            ToastSeverity.Error => TimeSpan.FromSeconds(10),
            ToastSeverity.Warning => TimeSpan.FromSeconds(8),
            ToastSeverity.Success => TimeSpan.FromSeconds(4),
            _ => TimeSpan.FromSeconds(5)
        };

    // ==========================================================
    //  Positioning
    // ==========================================================

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Top + 20;
    }

    private void UpdatePosition()
    {
        UpdateLayout();
        PositionWindow();
        SizeToContent = SizeToContent.WidthAndHeight;
    }
}
