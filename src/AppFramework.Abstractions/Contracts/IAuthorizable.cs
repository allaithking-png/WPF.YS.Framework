using System.Collections.Generic;

namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// قدرة العنصر على الإعلان عن الصلاحيات المطلوبة لعرضه/تشغيله.
/// </summary>
/// <remarks>
/// الـ Framework يفحص هذه الصلاحيات قبل عرض الشاشة أو تفعيل أمر.
/// </remarks>
public interface IAuthorizable : IAppAware
{
    /// <summary>الصلاحيات المطلوبة (كلها إلزامية).</summary>
    IReadOnlyCollection<string> RequiredPermissions { get; }
}
