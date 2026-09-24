using System;

namespace AppFramework.Abstractions.Attributes;

/// <summary>
/// وسم يعلن أن خاصية معيّنة يجب حفظها/استرجاعها عبر الجلسات.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PersistAttribute : Attribute
{
    /// <summary>مفتاح مخصص (افتراضيًا اسم الخاصية).</summary>
    public string? Key { get; set; }
}