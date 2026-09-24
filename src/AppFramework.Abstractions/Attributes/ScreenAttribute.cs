using System;

namespace AppFramework.Abstractions.Attributes;

/// <summary>
/// وسم يصف شاشة في النظام (يُستخدم في Auto-Scan).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ScreenAttribute : Attribute
{
    public string Id { get; }
    public string Title { get; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool SupportsMultiOpen { get; set; }
    public bool RestoreOnStartup { get; set; }
    public string? DataSourceKey { get; set; }
    public string[]? Permissions { get; set; }

    public ScreenAttribute(string id, string title)
    {
        Id = id;
        Title = title;
    }
}