using System;

namespace AppFramework.Data.Sources;

/// <summary>
/// إعدادات مصدر بيانات HTTP/REST.
/// </summary>
public interface IHttpDataSourceConfig
{
    /// <summary>مفتاح المصدر (مثال: "Orders").</summary>
    string Key { get; }

    /// <summary>المسار الأساسي (مثال: "https://api.example.com/orders").</summary>
    string BasePath { get; }

    /// <summary>مسارات فرعية مخصصة (اختياري).</summary>
    HttpDataSourceEndpoints Endpoints { get; }
}

/// <summary>
/// مسارات REST endpoints. القيم الافتراضية مبنية على نمط REST شائع.
/// </summary>
public sealed class HttpDataSourceEndpoints
{
    /// <summary>مسار جلب دفعة (افتراضي: "{BasePath}?page={page} pageSize={pageSize}").</summary>
    public string Batch { get; set; } = "{base}?page={page}&pageSize={pageSize}";

    /// <summary>مسار جلب المفاتيح فقط.</summary>
    public string Keys { get; set; } = "{base}/keys?page={page}&pageSize={pageSize}";

    /// <summary>مسار جلب عنصر واحد (افتراضي: "{BasePath}/{key}").</summary>
    public string Item { get; set; } = "{base}/{key}";

    /// <summary>مسار حفظ عنصر (افتراضي: PUT "{BasePath}/{key}").</summary>
    public string Save { get; set; } = "{base}/{key}";

    /// <summary>مسار حذف عنصر.</summary>
    public string Delete { get; set; } = "{base}/{key}";

    /// <summary>مسار حفظ دفعة.</summary>
    public string SaveBatch { get; set; } = "{base}/batch";
}
