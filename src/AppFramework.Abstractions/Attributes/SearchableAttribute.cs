using System;

namespace AppFramework.Abstractions.Attributes;

/// <summary>
/// يحدد أن الخاصية قابلة للبحث في شاشات القوائم.
/// </summary>
/// <remarks>
/// عند تطبيقه على خصائص DTO، يتم البحث فيها تلقائيًا
/// في شريط البحث في <c>BaseListViewModel</c>.
///
/// <code>
/// public sealed class CustomerDto
/// {
///     [Searchable]
///     public string Name { get; set; } = "";
///
///     [Searchable(Order = 1)]
///     public string Email { get; set; } = "";
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SearchableAttribute : Attribute
{
    /// <summary>ترتيب البحث (اختياري).</summary>
    public int Order { get; set; } = 0;

    /// <summary>وزن البحث (للترتيب حسب الأهمية — مستقبلاً).</summary>
    public int Weight { get; set; } = 1;

    public SearchableAttribute() { }
}