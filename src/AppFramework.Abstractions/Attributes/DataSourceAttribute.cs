using System;

namespace AppFramework.Abstractions.Attributes;

/// <summary>
/// وسم يربط كلاس DataSource بمفتاحه الفريد.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DataSourceAttribute : Attribute
{
    public string Key { get; }

    public DataSourceAttribute(string key)
    {
        Key = key;
    }
}