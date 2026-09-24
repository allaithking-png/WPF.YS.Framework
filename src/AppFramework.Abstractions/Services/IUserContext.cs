using System.Collections.Generic;

namespace AppFramework.Abstractions.Services;

/// <summary>
/// سياق المستخدم الحالي.
/// </summary>
public interface IUserContext
{
    /// <summary>معرّف المستخدم.</summary>
    string UserId { get; }

    /// <summary>الاسم المعروض.</summary>
    string DisplayName { get; }

    /// <summary>هل المستخدم مسجّل دخول؟</summary>
    bool IsAuthenticated { get; }

    /// <summary>صلاحيات المستخدم.</summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>هل يملك صلاحية معيّنة؟</summary>
    bool HasPermission(string permission);

    /// <summary>هل يملك أيًا من الصلاحيات المذكورة؟</summary>
    bool HasAnyPermission(params string[] permissions);
}