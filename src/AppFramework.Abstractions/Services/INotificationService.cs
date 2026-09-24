using System;
using System.Collections.Generic;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// خدمة الإشعارات.
/// </summary>
public interface INotificationService
{
    /// <summary>إشعار معلوماتي.</summary>
    void Info(string title, string message);

    /// <summary>إشعار نجاح.</summary>
    void Success(string title, string message);

    /// <summary>إشعار تحذير.</summary>
    void Warning(string title, string message);

    /// <summary>إشعار خطأ.</summary>
    void Error(string title, string message);

    /// <summary>إشعار مع أزرار استجابة (روابط/أوامر).</summary>
    void ShowWithActions(string title, string message, params NotificationAction[] actions);

    /// <summary>حدث عند الضغط على إشعار.</summary>
    event EventHandler<NotificationActionEventArgs>? ActionInvoked;
}

/// <summary>زر استجابة داخل إشعار.</summary>
public sealed record NotificationAction(string Label, Action? Callback = null, string? CommandKey = null);

/// <summary>حدث تنفيذ إجراء من إشعار.</summary>
public sealed record NotificationActionEventArgs(string NotificationTitle, NotificationAction Action);