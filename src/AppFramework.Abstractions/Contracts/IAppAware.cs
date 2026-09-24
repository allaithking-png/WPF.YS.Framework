namespace AppFramework.Abstractions.Contracts;

/// <summary>
/// نقطة الدخول الموحّدة لكل عنصر في الإطار.
/// أي ViewModel أو Screen أو Dialog ينفّذ هذه الواجهة سيحصل على الخدمات تلقائيًا
/// عندما يُنشئه الـ DI Container.
/// </summary>
/// <remarks>
/// هذه الواجهة **لا تفرض** أي Base Class. أي كلاس عادي يمكنه تنفيذها.
/// بعد الاستدعاء، يصبح لديه وصول كامل إلى <see cref="IServiceProvider"/>
/// ليحصل على أي خدمة يحتاجها.
/// </remarks>
public interface IAppAware
{
    /// <summary>
    /// يُستدعى مرة واحدة عند إنشاء العنصر عبر حاوية DI.
    /// لا تستدعِ هذه الدالة يدويًا — الإطار يستدعيها.
    /// </summary>
    /// <param name="services">مزوّد الخدمات المركزي.</param>
    void AttachServices(IServiceProvider services);
}
