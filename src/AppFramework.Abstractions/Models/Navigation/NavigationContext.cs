using System.Collections.Generic;

namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// سياق التنقل بين الشاشات.
/// </summary>
public sealed record NavigationContext(
    string TargetId,
    string? TargetLocation = null,
    IReadOnlyDictionary<string, object?>? Parameters = null,
    bool IsMultiOpen = false,
    string? InstanceId = null)
{
    /// <summary>مفتاح النسخة (InstanceKey) للتفريق بين نوافذ متعددة.</summary>
    public string InstanceKey => InstanceId ?? TargetId;
}