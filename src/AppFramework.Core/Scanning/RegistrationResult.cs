using System.Collections.Generic;

namespace AppFramework.Core.Scanning;

/// <summary>
/// نتيجة عملية Auto-Scan.
/// </summary>
public sealed class RegistrationResult
{
    /// <summary>الشاشات المسجّلة (ScreenId).</summary>
    public List<string> Screens { get; } = new();

    /// <summary>مصادر البيانات المسجّلة (SourceKey).</summary>
    public List<string> DataSources { get; } = new();

    /// <summary>الـ ViewModels المسجّلة (FullName).</summary>
    public List<string> ViewModels { get; } = new();

    /// <summary>عدد كل المكوّنات المسجّلة.</summary>
    public int TotalCount => Screens.Count + DataSources.Count + ViewModels.Count;

    /// <summary>ملخص نصّي مختصر.</summary>
    public override string ToString()
        => $"Screens: {Screens.Count}, DataSources: {DataSources.Count}, ViewModels: {ViewModels.Count}";
}