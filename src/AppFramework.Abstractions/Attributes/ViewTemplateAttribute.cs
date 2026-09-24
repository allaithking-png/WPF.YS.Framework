using System;
using AppFramework.Abstractions.Models.ViewTemplates;

namespace AppFramework.Abstractions.Attributes;

/// <summary>
/// وسم يعلن عن قالب عرض يدعمه ViewModel.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ViewTemplateAttribute : Attribute
{
    public ViewMode Mode { get; }
    public Type ViewType { get; }
    public string Title { get; set; } = "";
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }

    public ViewTemplateAttribute(ViewMode mode, Type viewType)
    {
        Mode = mode;
        ViewType = viewType;
    }
}