namespace AppFramework.Abstractions.Models.Navigation;

/// <summary>
/// نتيجة محاولة التنقل.
/// </summary>
public sealed record NavigationResult(
    bool Success,
    string? TargetId = null,
    string? ErrorMessage = null)
{
    public static NavigationResult Ok(string? targetId = null)
        => new(true, targetId);

    public static NavigationResult Fail(string message, string? targetId = null)
        => new(false, targetId, message);
}