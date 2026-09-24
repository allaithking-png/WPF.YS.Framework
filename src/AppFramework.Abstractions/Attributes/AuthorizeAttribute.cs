using System;

namespace AppFramework.Abstractions.Attributes;

/// <summary>
/// وسم يحدد الصلاحيات المطلوبة لعرض/تشغيل عنصر.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    public string[] Permissions { get; }

    public AuthorizeAttribute(params string[] permissions)
    {
        Permissions = permissions ?? Array.Empty<string>();
    }
}